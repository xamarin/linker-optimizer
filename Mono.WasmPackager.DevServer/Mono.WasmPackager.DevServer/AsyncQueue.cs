using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Mono.WasmPackager.DevServer
{
	class AsyncQueue<T> : IDisposable
		where T : class
	{
		const int DefaultTimeout = -1;
		const int MainLoopTimeout = 15;
		const int HandlerTimout = 60;

		readonly List<Task> pending;

		readonly ConcurrentQueue<QueueEntry> queue;
		readonly AutoResetEvent queueEvent;
		readonly bool allowParallel;
		readonly CancellationTokenSource cts;
		Func<T, CancellationToken, Task> handler;
		int disposed;
		int running;

		class QueueEntry {
			public T Item {
				get;
			}

			public TaskCompletionSource<object> Completed {
				get;
			}

			public QueueEntry (T item)
			{
				Item = item;
				Completed = new TaskCompletionSource<object> ();
			}
		}

		public delegate Task AsyncQueueHandler (T args, CancellationToken token);

		public string Name {
			get;
		}

		public AsyncQueue (bool allowParallel, string name)
		{
			this.allowParallel = allowParallel;
			Name = name;

			queue = new ConcurrentQueue<QueueEntry> ();
			queueEvent = new AutoResetEvent (false);
			cts = new CancellationTokenSource ();
			pending = new List<Task> ();
		}

		public void Start (Func<T, CancellationToken, Task> handler)
		{
			if (Interlocked.CompareExchange (ref running, 1, 0) != 0)
				throw new InvalidOperationException ("Async queue already running.");
			this.handler = handler;

			MainLoop ();
		}

		public async Task EnqueueAsync (T args)
		{
			Log ($"ENQUEUE ASYNC: {args}");
			var item = new QueueEntry (args);
			queue.Enqueue (item);
			queueEvent.Set ();
			await item.Completed.Task.ConfigureAwait (false);
			Log ($"ENQUEUE ASYNC DONE: {args}");
		}

		public void Enqueue (T args)
		{
			Log ($"ENQUEUE: {args}");
			queue.Enqueue (new QueueEntry (args));
			queueEvent.Set ();
		}

		Task<QueueEntry> WaitForEventInternal ()
		{
			return WaitHandleHelper.WaitForHandle (queueEvent, () => {
				if (queue.TryDequeue (out var result))
					return result;
				return null;
			}, DefaultTimeout, cts.Token);
		}

		async Task<QueueEntry> WaitForEvent ()
		{
			while (!cts.Token.IsCancellationRequested) {
				if (queue.TryDequeue (out var entry))
					return entry;

				entry = await WaitForEventInternal ().ConfigureAwait (false);
				if (entry != null)
					return entry;

				Log ($"MAIN LOOP - GOT NULL");
			}

			return null;
		}

		async Task<QueueEntry> HandleEvent (QueueEntry entry)
		{
			Log ($"HANDLE EVENT: {entry.Item}");
			var handlerTask = handler (entry.Item, cts.Token);
			while (!cts.Token.IsCancellationRequested) {
				var handlerTimeout = Task.Delay (TimeSpan.FromSeconds (HandlerTimout));
				var handlerRes = await Task.WhenAny (handlerTimeout, handlerTask);

				if (handlerRes == handlerTimeout) {
					Log ($"HANDLER TIMEOUT: {entry.Item}");
				} else {
					Log ($"HANDLER COMPLETE: {entry.Item}");
					entry.Completed.TrySetResult (null);
					break;
				}
			}

			return entry;
		}

		public async void MainLoop ()
		{
			pending.Add (WaitForEvent ());
			pending.Add (Task.Delay (MainLoopTimeout));

			while (!cts.Token.IsCancellationRequested) {
				Log ($"MAIN LOOP");
				var task = await Task.WhenAny (pending);
				if (task == pending [1]) {
					Log ($"MAIN LOOP - STILL RUNNING");
					pending [1] = Task.Delay (TimeSpan.FromSeconds (MainLoopTimeout));
					continue;
				}

				var entry = ((Task<QueueEntry>)task).Result;
				if (task == pending [0]) {
					pending [0] = WaitForEvent ();
				} else {
					Log ($"MAIN LOOP - BACKGROUND TASK COMPLETED: {entry}");
					var idx = pending.IndexOf (task);
					pending.RemoveAt (idx);
					continue;
				}

				cts.Token.ThrowIfCancellationRequested ();

				if (!allowParallel) {
					await HandleEvent (entry);
					continue;
				}

				pending.Add (HandleEvent (entry));
			}
		}

		protected virtual void Log (string msg)
		{
			Debug.WriteLine ($"[AsyncQueue:{typeof (T).Name}:{Name}:{queue.Count}:{pending.Count}]: {msg}");
		}

		public void Dispose ()
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) != 0)
				return;

			cts.Cancel ();
			cts.Dispose ();
		}
	}
}
