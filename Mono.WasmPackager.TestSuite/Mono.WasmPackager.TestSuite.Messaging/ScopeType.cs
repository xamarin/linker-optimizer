namespace Mono.WasmPackager.TestSuite.Messaging.Debugger
{
	public enum ScopeType
	{
		Global,
		Local,
		With,
		Closure,
		Catch,
		Block,
		Script,
		Eval,
		Module,
		WasmExpressionStack
	}
}
