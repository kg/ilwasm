@del /F/Q output\*
@msbuild WasmSExprEmitter.sln /p:Platform="Any CPU" /p:Configuration="Debug" /v:m