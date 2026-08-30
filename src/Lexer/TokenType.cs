namespace DMToCSharp.Lexer
{
    public enum TokenType
    {
        // Special
        EOF,
        Newline,
        Indent,
        Dedent,
        Error,

        // Literals
        Identifier,
        Number,
        String,
        VerbatimString,
        Resource,
        Path,

        // Keywords
        Var,
        Proc,
        Verb,
        Set,
        As,
        In,
        To,
        Step,
        If,
        Else,
        While,
        Do,
        For,
        Switch,
        Return,
        Break,
        Continue,
        Spawn,
        Try,
        Catch,
        Throw,
        New,
        Del,
        Goto,
        Global,
        Const,
        Static,
        Tmp,
        Null,
        True,
        False,
        Usr,
        Src,
        Args,
        World,
        Locate,
        Input,
        Initial,
        IsType,

        // Punctuators
        LeftParen,          // (
        RightParen,         // )
        LeftBracket,        // [
        RightBracket,       // ]
        LeftBrace,          // {
        RightBrace,         // }
        Comma,              // ,
        Semicolon,          // ;
        Colon,              // :
        Dot,                // .
        DotDot,             // ..
        DotDotDot,          // ...
        Question,           // ?
        Tilde,              // ~

        // Operators
        Plus,               // +
        Minus,              // -
        Multiply,           // *
        Divide,             // /
        Modulo,             // %
        Power,              // **
        Bang,               // !
        Equal,              // ==
        NotEqual,           // !=
        Equivalent,         // ~=
        NotEquivalent,      // ~!
        Less,               // <
        LessEqual,          // <=
        Greater,            // >
        GreaterEqual,       // >=
        LogicalAnd,         // &&
        LogicalOr,          // ||
        BitwiseAnd,         // &
        BitwiseOr,          // |
        BitwiseXor,         // ^
        ShiftLeft,          // <<
        ShiftRight,         // >>

        // Assignments
        Assign,             // =
        AddAssign,          // +=
        SubtractAssign,     // -=
        MultiplyAssign,     // *=
        DivideAssign,       // /=
        ModuloAssign,       // %=
        BitwiseAndAssign,   // &=
        BitwiseOrAssign,    // |=
        BitwiseXorAssign,   // ^=
        ShiftLeftAssign,    // <<=
        ShiftRightAssign,   // >>=
        Increment,          // ++
        Decrement           // --
    }
}
