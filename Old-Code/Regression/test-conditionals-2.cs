using System;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	class TestUnicode
	{
		ISimpleCollator collator;
		static Dictionary<string, ISimpleCollator> collators;
		const string m_name = "test";
		const string _sortName = "test";

		public static void Main ()
		{
			var test = new TestUnicode ();
			var collator = test.GetCollator ();
			if (collator != null)
				throw new AssertionException ("GetCollator() should have returned null.");

			if (MonoLinkerSupport.IsTypeAvailable<SimpleCollator> ())
				throw new AssertionException ("SimpleCollator should have been removed.");
		}

		ISimpleCollator GetCollator ()
		{
			if (!MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Martin))
				return null;

			if (collator != null)
				return collator;

			if (collators == null) {
				Interlocked.CompareExchange (ref collators, new Dictionary<string, ISimpleCollator> (StringComparer.Ordinal), null);
			}

			TestHelpers.AssertRemoved ();

			lock (collators) {
				if (!collators.TryGetValue (_sortName, out collator)) {
					collator = new SimpleCollator (CultureInfo.GetCultureInfo (m_name));
					collators [_sortName] = collator;
				}
			}

			return collator;
		}
	}

	interface ISimpleCollator
	{
	}

	class SimpleCollator : ISimpleCollator
	{
		public SimpleCollator (CultureInfo culture)
		{ }
	}
}
