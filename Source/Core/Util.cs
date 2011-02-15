//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
namespace Microsoft.Boogie {
  using System;
  using System.IO;
  using System.Collections;
  using System.Diagnostics.Contracts;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  public static class LinqExtender
  {
    public static string Concat(this IEnumerable<string> strings, string separator)
    {
      var sb = new StringBuilder();
      var first = true;
      foreach (var s in strings) {
        if (!first)
          sb.Append(separator);
        first = false;
        sb.Append(s);
      }
      return sb.ToString();
    }

    public static string MapConcat<T>(this IEnumerable<T> objects, Func<T,string> toString, string separator)
    {
      var sb = new StringBuilder();
      var first = true;
      foreach (var s in objects) {
        if (!first)
          sb.Append(separator);
        first = false;
        sb.Append(toString(s));
      }
      return sb.ToString();
    }

    public static IEnumerable<T> SkipEnd<T>(this IEnumerable<T> source, int count)
    {
      var l = source.ToList();
      if (count >= l.Count)
        return Enumerable.Empty<T>();
      l.RemoveRange(l.Count - count, count);
      return l;
    }
  }

  public class TokenTextWriter : IDisposable {
    string/*!*/ filename;
    TextWriter/*!*/ writer;
    [ContractInvariantMethod]
    void ObjectInvariant() {
      Contract.Invariant(filename != null);
      Contract.Invariant(writer != null);
    }

    bool setTokens = true;
    int line = 1;
    int col;

    private const int indent_size = 2;
    protected static string Indent(int level) {
      Contract.Ensures(Contract.Result<string>() != null);
      return new string(' ', (indent_size * level));
    }


    // Keywords, this array *must* be sorted
    public static readonly string[]/*!*/ BplKeywords =
        {
                "assert",
                "assume",
                "axiom",
                "bool",
                "break",
                "call",
                "cast",
                "const",
                "else",
                "ensures",
                "exists",
                "false",
                "forall",
                "free",
                "function",
                "goto",
                "havoc",
                "if",
                "implementation",
                "int",
                "invariant",
                "modifies",
                "old",
                "procedure",
                "public",
                "requires",
                "return",
                "returns",
                "true",
                "type",
                "unique",
                "var",
                "where",
                "while",
        };

    private IToken/*!*/ CurrentToken {
      get {
        Contract.Ensures(Contract.Result<IToken>() != null);

        Token token = new Token();
        token.filename = filename;
        token.line = line;
        token.col = col;
        return token;
      }
    }

    public void SetToken(Absy absy) {
      Contract.Requires(absy != null);
      this.SetToken(ref absy.tok);
    }

    public void SetToken(ref IToken tok) {
      Contract.Requires(tok != null);
      if (this.setTokens) {
        tok = this.CurrentToken;
      }
    }

    public static string SanitizeIdentifier(string name) {
      Contract.Requires(name != null);
      Contract.Ensures(Contract.Result<string>() != null);
      int index = Array.BinarySearch(TokenTextWriter.BplKeywords, name);
      if (index >= 0) {
        return "\\" + name;
      } else if (name.Length > 2 && name[0] == 'b' && name[1] == 'v') {
        int dummy;
        return int.TryParse(name.Substring(2), out dummy) ? "\\" + name : name;
      } else {
        return name;
      }
    }

    public TokenTextWriter(string filename) :base(){//BASEMOVE DANGER
      Contract.Requires(filename != null);
      this.filename = filename;
      this.writer = new StreamWriter(filename);
      //base();
    }

    public TokenTextWriter(string filename, TextWriter writer, bool setTokens)
      : base() {//BASEMOVE DANGER
      Contract.Requires(writer != null);
      Contract.Requires(filename != null);
      this.filename = filename;
      this.writer = writer;
      this.setTokens = setTokens;
      //base();
    }

    public TokenTextWriter(string filename, TextWriter writer)
      : base() {//BASEMOVE DANGER
      Contract.Requires(writer != null);
      Contract.Requires(filename != null);
      this.filename = filename;
      this.writer = writer;
      //base();
    }

    public TokenTextWriter(TextWriter writer)
      : base() {//BASEMOVE DANGER
      Contract.Requires(writer != null);
      this.filename = "<no file>";
      this.writer = writer;
      //base();
    }

    public void Write(string text) {
      Contract.Requires(text != null);
      this.writer.Write(text);
      this.col += text.Length;
    }

    public void WriteIndent(int level) {
      this.Write(Indent(level));
    }

    public void Write(string text, params object[] args) {
      Contract.Requires(text != null);
      this.Write(string.Format(text, args));
    }

    public void Write(int level, string text) {
      Contract.Requires(text != null);
      this.WriteIndent(level);
      this.Write(text);
    }

    public void Write(int level, string text, params object[] args) {
      Contract.Requires(text != null);
      this.WriteIndent(level);
      this.Write(text, args);
    }

    public void Write(Absy node, string text) {
      Contract.Requires(text != null);
      Contract.Requires(node != null);
      this.SetToken(node);
      this.Write(text);
    }

    public void Write(Absy node, string text, params string[] args) {
      Contract.Requires(text != null);
      Contract.Requires(node != null);
      this.SetToken(node);
      this.Write(text, args);
    }

    public void Write(Absy node, int level, string text) {
      Contract.Requires(text != null);
      Contract.Requires(node != null);
      this.WriteIndent(level);
      this.SetToken(node);
      this.Write(text);
    }

    public void Write(Absy node, int level, string text, params object[] args) {
      Contract.Requires(text != null);
      Contract.Requires(node != null);
      this.WriteIndent(level);
      this.SetToken(node);
      this.Write(text, args);
    }

    public void WriteLine() {
      this.writer.WriteLine();
      this.line++;
      this.col = 0;
    }

    public void WriteLine(string text) {
      Contract.Requires(text != null);
      this.writer.WriteLine(text);
      this.line++;
      this.col = 0;
    }

    public void WriteText(string text) {
      Contract.Requires(text != null);
      int processed = 0;
      while (true) {
        int n = text.IndexOf('\n', processed);
        if (n == -1) {
          this.writer.Write(text);
          this.col += text.Length - processed;
          return;
        }
        processed = n + 1;
        this.line++;
        this.col = 0;
      }
    }

    public void WriteLine(string text, params object[] args) {
      Contract.Requires(text != null);
      this.WriteLine(string.Format(text, args));
    }

    public void WriteLine(int level, string text) {
      Contract.Requires(text != null);
      this.WriteIndent(level);
      this.WriteLine(text);
    }

    public void WriteLine(int level, string text, params object[] args) {
      Contract.Requires(text != null);
      this.WriteIndent(level);
      this.WriteLine(text, args);
    }

    public void WriteLine(Absy node, string text) {
      Contract.Requires(text != null);
      Contract.Requires(node != null);
      this.SetToken(node);
      this.WriteLine(text);
    }

    public void WriteLine(Absy node, int level, string text) {
      Contract.Requires(text != null);
      Contract.Requires(node != null);
      this.SetToken(node);
      this.WriteLine(level, text);
    }

    public void WriteLine(Absy node, int level, string text, params object[] args) {
      Contract.Requires(text != null);
      Contract.Requires(node != null);
      this.SetToken(node);
      this.WriteLine(level, text, args);
    }

    public void Close() {
      this.writer.Close();
    }

    public void Dispose() {
      this.Close();
    }
  }

  public class Helpers {
    public static string BeautifyBplString(string s) {
      Contract.Requires(s != null);
      Contract.Ensures(Contract.Result<string>() != null);
      // strip "^" if it is the first character, change "$result" to "result"
      if (s.StartsWith("^") || s == "$result") {
        s = s.Substring(1);
      } else if (s.StartsWith("call")) {
        s = s.Substring(s.IndexOf('@') + 1);
        if (s.StartsWith("formal@")) {
          s = "(value of formal parameter: " + s.Substring(7) + ")";
        }
      }
      // strip "$in" from the end of identifier names
      if (s.EndsWith("$in")) {
        return "(initial value of: " + s.Substring(0, s.Length - 3) + ")";
      } else {
        return s;
      }
    }
    public static string PrettyPrintBplExpr(Expr e) {
      Contract.Requires(e != null);
      Contract.Ensures(Contract.Result<string>() != null);
      // anything that is unknown will just be printed via ToString
      // OldExpr and QuantifierExpr, BvExtractExpr, BvConcatExpr are ignored for now
      // LiteralExpr is printed as itself by ToString
      if (e is IdentifierExpr) {
        string s = e.ToString();
        return Helpers.BeautifyBplString(s);
      } else if (e is NAryExpr) {
        NAryExpr ne = (NAryExpr)e;
        IAppliable fun = ne.Fun;
        ExprSeq eSeq = ne.Args;
        if (fun != null) {
          if ((fun.FunctionName == "$Length" || fun.FunctionName == "$StringLength") && eSeq.Length == 1) {
            Expr e0 = eSeq[0];
            if (e0 != null) {
              string s0 = PrettyPrintBplExpr(e0);
              return s0 + ".Length";
            }
            //unexpected, just fall outside to the default
          } else if (fun.FunctionName == "$typeof" && eSeq.Length == 1) {
            Expr e0 = eSeq[0];
            if (e0 != null) {
              string s0 = PrettyPrintBplExpr(e0);
              return "(the dynamic type of: " + s0 + ")";
            }
            //unexpected, just fall outside to the default
          } else if (fun.FunctionName == "IntArrayGet" && eSeq.Length == 2) {
            Expr e0 = eSeq[0];
            Expr e1 = eSeq[1];
            if (e0 != null && e1 != null) {
              string s0 = PrettyPrintBplExpr(e0);
              string s1 = PrettyPrintBplExpr(e1);
              return s0 + "[" + s1 + "]";
            }
            //unexpected, just fall outside to the default
          } else if (fun.FunctionName == "$Is" && eSeq.Length == 2) {
            Expr e0 = eSeq[0];
            Expr e1 = eSeq[1];
            if (e0 != null && e1 != null) {
              string s0 = PrettyPrintBplExpr(e0);
              string s1 = PrettyPrintBplExpr(e1);
              return "(" + s0 + " == null || (" + s0 + " is " + s1 + "))";
            }
            //unexpected, just fall outside to the default
          } else if (fun.FunctionName == "$IsNotNull" && eSeq.Length == 2) {
            Expr e0 = eSeq[0];
            Expr e1 = eSeq[1];
            if (e0 != null && e1 != null) {
              string s0 = PrettyPrintBplExpr(e0);
              string s1 = PrettyPrintBplExpr(e1);
              return "(" + s0 + " is " + s1 + ")";
            }
            //unexpected, just fall outside to the default
          } else if (fun is MapSelect && eSeq.Length <= 3) {
            // only maps with up to two arguments are supported right now (here)
            if (cce.NonNull(eSeq[0]).ToString() == "$Heap") {
              //print Index0.Index1, unless Index1 is "$elements", then just print Index0
              string s0 = PrettyPrintBplExpr(cce.NonNull(eSeq[1]));
              if (eSeq.Length > 2) {
                string s1 = PrettyPrintBplExpr(cce.NonNull(eSeq[2]));
                if (s1 == "$elements") {
                  return s0;
                } else {
                  if (eSeq[2] is IdentifierExpr) {
                    // strip the class name out of a fieldname
                    s1 = s1.Substring(s1.LastIndexOf('.') + 1);
                  }
                  return s0 + "." + s1;
                }
              }
            }
            //unexpected, just fall outside to the default
          } else if (fun is Microsoft.Boogie.BinaryOperator && eSeq.Length == 2) {
            Microsoft.Boogie.BinaryOperator f = (Microsoft.Boogie.BinaryOperator)fun;
            Expr e0 = eSeq[0];
            Expr e1 = eSeq[1];
            if (e0 != null && e1 != null) {
              string s0 = PrettyPrintBplExpr(e0);
              string s1 = PrettyPrintBplExpr(e1);
              string op = "";
              switch (f.Op) {
                case Microsoft.Boogie.BinaryOperator.Opcode.Add:
                  op = " + ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.And:
                  op = " && ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Div:
                  op = " / ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Eq:
                  op = " == ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Ge:
                  op = " >= ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Gt:
                  op = " > ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Iff:
                  op = " <==> ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Imp:
                  op = " ==> ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Le:
                  op = " <= ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Lt:
                  op = " < ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Mod:
                  op = " % ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Mul:
                  op = " * ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Neq:
                  op = " != ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Or:
                  op = " || ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Sub:
                  op = " - ";
                  break;
                case Microsoft.Boogie.BinaryOperator.Opcode.Subtype:
                  op = " <: ";
                  break;
                default:
                  op = " ";
                  break;
              }
              return "(" + s0 + op + s1 + ")";
            }
            //unexpected, just fall outside to the default
          } else {
            string s = fun.FunctionName + "(";
            for (int i = 0; i < eSeq.Length; i++) {
              Expr ex = eSeq[i];
              Contract.Assume(ex != null);
              if (i > 0) {
                s += ", ";
              }
              string t = PrettyPrintBplExpr(ex);
              if (t.StartsWith("(") && t.EndsWith(")")) {
                t = t.Substring(1, t.Length - 2);
              }
              s += t;
            }
            s += ")";
            return s;
            //unexpected, just fall outside to the default
          }
        }
      }

      return e.ToString();
    }

    private static readonly DateTime StartUp = DateTime.Now;

    public static void ExtraTraceInformation(string point) {
      Contract.Requires(point != null);
      if (CommandLineOptions.Clo.TraceTimes) {
        DateTime now = DateTime.Now;
        TimeSpan timeSinceStartUp = now - StartUp;
        Console.WriteLine(">>> {0}   [{1} s]", point, timeSinceStartUp.TotalSeconds);
      }
    }

    // Substitute @PROC@ in a filename with the given descName
    public static string SubstituteAtPROC(string descName, string fileName) {
      Contract.Requires(fileName != null);
      Contract.Requires(descName != null);
      Contract.Ensures(Contract.Result<string>() != null);
      System.Text.StringBuilder/*!*/ sb =
        new System.Text.StringBuilder(descName.Length);
      // quote the name, characters like ^ cause trouble in CMD
      // while $ could cause trouble in SH
      foreach (char c in descName) {
        if (Char.IsLetterOrDigit(c) || c == '.') {
          sb.Append(c);
        } else {
          sb.Append('_');
        }
      }
      string pn = sb.ToString();
      // We attempt to avoid filenames that are too long, but we only
      // do it by truncating the @PROC@ replacement, which leaves unchanged
      // any filename extension specified by the user.  We base our
      // calculations on that there is at most one occurrence of @PROC@.
      if (180 <= fileName.Length - 6 + pn.Length) {
        pn = pn.Substring(0, Math.Max(180 - (fileName.Length - 6), 0)) + "-n" + sequenceNumber;
        sequenceNumber++;
      }

      return fileName.Replace("@PROC@", pn);
    }

    private static int sequenceNumber = 0;

  }
}
