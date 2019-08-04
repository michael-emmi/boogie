//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------

namespace Microsoft.Boogie {
  using System;
  using System.Linq;
  using System.IO;
  using System.Collections.Generic;

  public class BoogieServer
  {

    public static int Main(string[] args)
    {
      CommandLineOptions.Install(new CommandLineOptions());
      ExecutionEngine.printer = new ConsolePrinter();
      if (!CommandLineOptions.Clo.Parse(args)) {
          ExecutionEngine.printer.ErrorWriteLine(Console.Out, "*** Error: BLAH");
          return 1;
      }

      Listen();
      return 0;
    }

    public static void Listen() {
      Console.WriteLine("Boogie server started.");

      while (true) {
        var line = Console.ReadLine();
        if (line == null)
          break;

        var files = line.Split(' ').Select(Path.GetFullPath).ToList();
        ProcessQuery(files);
      }

      Console.WriteLine("Boogie server reached end of input.");
    }

    static void ProcessQuery(List<string> files) {
        foreach (string file in files) {
          string extension = Path.GetExtension(file);
          extension = extension == null ? String.Empty : extension.ToLower();

          if (extension != ".bpl") {
            ExecutionEngine.printer.ErrorWriteLine(
              Console.Out,
              "*** Error: '{0}': Filename extension '{1}' is not supported. Input files must be BoogiePL programs (.bpl).",
              file,
              extension);
            return;
          }
        }

        ExecutionEngine.ProcessFiles(files, false);
    }
  }
}
