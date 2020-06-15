using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.WasmPackager.DevServer
{
	public static class WaitHandleHelper
	{
		public static async Task<T> WaitForHandle<T> (WaitHandle handle, Func<T> func, int timeout, CancellationToken token)
		{
			RegisteredWaitHandle registeredHandle = null;
			var tcs = new TaskCompletionSource<T> ();
			var tokenRegistration = default (CancellationTokenRegistration);

			try {
				registeredHandle = ThreadPool.RegisterWaitForSingleObject (
					handle,
					(_, timedOut) => {
						if (timedOut) {
							tcs.TrySetCanceled ();
							return;
						}
						try {
							var result = func ();
							tcs.TrySetResult (result);
						} catch (OperationCanceledException) {
							tcs.TrySetCanceled ();
						} catch (Exception e) {
							tcs.TrySetException (e);
						}
					},
					null,
					timeout,
					true);
				tokenRegistration = token.Register (_ => tcs.TrySetResult (default), null);
				return await tcs.Task;
			} catch (TaskCanceledException) {
				return default;
			} finally {
				if (registeredHandle != null)
					registeredHandle.Unregister (null);
				tokenRegistration.Dispose ();
			}
		}
	}
}
