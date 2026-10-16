abstract record ParserSymbol(Position Position);
record PrimitiveParserSymbol(string TypeName, Position Position) : ParserSymbol(Position);
record ArrayParserSymbol(ParserSymbol TypeSymbol, Position Position) : ParserSymbol(Position);
record FunctionParserSymbol(List<ParserSymbol> ParameterTypes, Position Position) : ParserSymbol(Position);