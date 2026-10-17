Globals.Init();

var compiler = new Compiler(new InvalidSymbol(), [], [], [], Globals.Classes);

compiler.CompileFile("D:/2020s Archive/2026/October/Code/Zaperol/main.zap", null);
compiler.RequireReturn(null);
compiler.Chunk.ConvertListsToArrays();

compiler.Chunk.Instructions.ForEach(x => Console.WriteLine(x));

await Vm.ExecuteAsync(compiler.Chunk);