using SharpLox.Core.Interface;
using SharpLox.Core.Parsing;
using SharpLox.Core.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpLox;

public class SharpLox {
    private static readonly Interpreter interpreter = new Interpreter();
    static bool hadError = false;
    static bool hadRuntimeError = false;
    static bool promptCheck = false;

    public static void Main(string[] args) {
        if (args.Length > 1) {
            Console.WriteLine("Usage: SharpLox [script]");
            // Fix: Use System.Environment to avoid conflict with our Environment class
            System.Environment.Exit(64);
        } else if (args.Length == 1) {
            if (args[0].EndsWith(".slx")) {
                RunFile(args[0]);
            } else {
                Console.WriteLine("\"" + args[0] + "\" is not a valid .slx file!");
                Console.WriteLine("Press Enter key to continue...");
                try {
                    Console.Read();
                    System.Environment.Exit(0);
                } catch {
                    Console.Error.WriteLine("Well... that's weird.");
                }
            }
        } else {
            RunPrompt();
        }
    }

    public static void ExitProgram() {
        Console.WriteLine("\nExiting...");
        System.Environment.Exit(0);
    }

    public static void FinishProgram() {
        try {
            System.Threading.Thread.Sleep(500);
        } catch {
            Console.Error.WriteLine("Well... that's weird.");
        }
        Console.WriteLine("\nProgram Finished");
        System.Environment.Exit(0);
    }

    private static void RunFile(string path) {
        promptCheck = false;
        byte[] bytes = File.ReadAllBytes(path);
        Run(Encoding.Default.GetString(bytes));

        if (hadError) System.Environment.Exit(65);
        if (hadRuntimeError) System.Environment.Exit(70);
    }

    private static void RunPrompt() {
        Console.WriteLine("SharpLox V1.2.1 REPL, exit(); to exit.");
        promptCheck = true;
        for (; ; )
        {
            Console.Write(" #lox » ");
            string line = Console.ReadLine();
            if (line == null) break;
            Run(line);
            hadError = false;
        }
    }

    public static void Run(string source) {
        Scanner scanner = new Scanner(source);
        List<Token> tokens = scanner.ScanTokens();
        Parser parser = new Parser(tokens);
        List<Stmt> statements = parser.Parse();

        if (hadError) return;

        Resolver resolver = new Resolver(interpreter);
        resolver.Resolve(statements);

        if (hadError) return;

        interpreter.Interpret(statements);

        if (!promptCheck) {
            FinishProgram();
        }
    }

    public static void Error(int line, string message) {
        Report(line, "", message);
    }

    private static void Report(int line, string where, string message) {
        Console.Error.WriteLine("[line " + line + "] Error" + where + ": " + message);
        hadError = true;
    }

    public static void Error(Token token, string message) {
        if (token.Type == TokenType.EOF) {
            Report(token.Line, " at end", message);
        } else {
            Report(token.Line, " at '" + token.Lexeme + "'", message);
        }
    }

    public static void RuntimeError(RuntimeError error) {
        Console.Error.WriteLine(error.Message + "\n[line " + error.Token.Line + "]");
        hadRuntimeError = true;
    }
}