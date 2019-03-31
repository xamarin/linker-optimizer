using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Activation;
using System.Runtime.CompilerServices;

namespace Martin.LinkerTest
{
	static class TestConditionals
	{
		public static void Main ()
		{
			RunFeature ();
		}

		static void RunFeature ()
		{
			CreateInstanceImpl (typeof (TestConditionals), BindingFlags.Default, null, null, null);
		}

		internal const int LookupMask = 0x000000FF;
		internal const BindingFlags ConstructorDefault= BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

		static Object CreateInstanceImpl (Type type,
						  BindingFlags bindingAttr, Binder binder,
						  Object[] args,
						  Object[] activationAttributes)
		{
			if ((object)type == null)
				throw new ArgumentNullException ("type");
			if (MonoLinkerSupport.IsWeakInstanceOf<TypeBuilder> (type))
				throw new NotSupportedException (MyEnvironment.GetResourceString ("X"));

			// If they didn't specify a lookup, then we will provide the default lookup.
			if ((bindingAttr & (BindingFlags) LookupMask) == 0)
				bindingAttr |= TestConditionals.ConstructorDefault;

			if (MonoLinkerSupport.IsFeatureSupported (MonoLinkerFeature.Remoting)) {
				if (activationAttributes != null && activationAttributes.Length > 0) {
					if (type.IsMarshalByRef) {
						if (!(type.IsContextful)) {
							if (activationAttributes.Length > 1 || !(activationAttributes[0] is UrlAttribute))
								throw new NotSupportedException (MyEnvironment.GetResourceString ("Y"));
						}
					} else {
						throw new NotSupportedException (MyEnvironment.GetResourceString ("Z"));
					}
				}
			}

			TypeInfo rt = type.UnderlyingSystemType as TypeInfo;

			if (rt == null)
				throw new ArgumentException (MyEnvironment.GetResourceString ("X"), "type");

			return Test (rt, bindingAttr, binder, args, activationAttributes);
		}

		static object Test (TypeInfo type, BindingFlags bindingAttr, Binder binder, Object[] args, Object[] activationAttributes)
		{
			return null;
		}
	}

	static class MyEnvironment
	{
		public static string GetResourceString (string text)
		{
			return text;
		}
	}
}
