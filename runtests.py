#!/usr/bin/env python

import platform
import os
import os.path
import unittest
import subprocess
import glob
import sys
import shutil
import re

def get_libs(fileName):
  with open(fileName, "r") as f:
    text = f.read()

  libComments = re.finditer("//#use (.*)", text)

  return list(
    os.path.join(os.path.dirname(fileName), match.group(1)) for match in libComments
  )

def get_stdout_path(fileName):
  with open(fileName, "r") as f:
    text = f.read()

  m = re.search("SetStdout\\(\"([^\"]*)\"\\);", text)

  if m:
    return m.group(1)
  else:
    return None

def compile_cs(fileName):
  if platform.system() == "Windows":
    csc = "csc"
  else:
    csc = "mcs"

  testName = os.path.basename(fileName)
  compiledPath = os.path.join("output", testName.replace(".cs", ".exe"))

  libs = get_libs(fileName)

  inTime = os.path.getmtime(fileName)

  try:
    outTime = os.path.getmtime(compiledPath)
  except OSError:
    outTime = 0
    pass

  isNewer = (outTime > inTime)

  for libName in libs:
    if os.path.getmtime(libName) > outTime:
      isNewer = False
      break

  if isNewer:
    return (0, compiledPath, get_stdout_path(fileName))

  fileNames = [fileName] + libs

  commandStr = ("%s /nologo /langversion:6 /debug+ /unsafe+ /debug:full %s /reference:%s /out:%s") % (
    csc, " ".join(fileNames), 
    os.path.join("WasmMeta", "bin", "WasmMeta.dll"),
    compiledPath
  )

  exitCode = subprocess.call(commandStr, shell=True)

  if exitCode != 0:
    print("failed while running '%s'" % commandStr)
    print("")

  return (exitCode, compiledPath, get_stdout_path(fileName))

def translate(compiledPath):
  wasmPath = compiledPath.replace(".exe", ".wasm")

  inTime = os.path.getmtime(compiledPath)
  try:
    outTime = os.path.getmtime(wasmPath)
    if (outTime > inTime):
      return (0, wasmPath)
  except OSError:
    pass

  commandStr = ("mono %s ilwasm.jsilconfig --quiet --nodefaults --e=WasmSExpr --outputFile=%s %s") % (
    os.path.join("third_party", "JSIL", "bin", "JSILc.exe"),
    wasmPath, compiledPath
  )
  exitCode = subprocess.call(commandStr, shell=True)

  if exitCode != 0:
    print("failed while running '%s'" % commandStr)
    print("")

  return (exitCode, wasmPath)

def run_csharp(compiledPath):
  commandStr = ("mono %s --quiet") % (compiledPath)
  exitCode = subprocess.call(commandStr, shell=True)

  if exitCode != 0:
    print("failed while running '%s'" % commandStr)
    print("")

  return exitCode

def run_wasm(wasmPath, stdoutPath):
  interpreterPath = os.path.realpath(os.path.join("third_party", "wasm-spec", "ml-proto", "_build", "host", "main.d.byte"))

  commandStr = ("%s %s") % (interpreterPath, wasmPath)

  csDataPath = None
  wasmDataPath = None
  csBytes = None
  wasmBytes = None

  if stdoutPath:
    csDataPath = os.path.join("output", "cs-data", stdoutPath)
    wasmDataPath = os.path.join("output", "wasm-data", stdoutPath)
    commandStr += " > " + wasmDataPath

  exitCode = subprocess.call(commandStr, shell=True)

  if exitCode != 0:
    print("failed while running '%s'" % commandStr)
    print("")

  if stdoutPath:
    with open(csDataPath, "rb") as f:
      csBytes = f.read()

    with open(wasmDataPath, "rb") as f:
      wasmBytes = f.read()

  return (exitCode, csBytes, wasmBytes)


class RunTests(unittest.TestCase):
  def _runTestFile(self, fileName):
    (exitCode, compiledPath, stdoutPath) = compile_cs(fileName)
    self.assertEqual(0, exitCode, "C# compiler failed with exit code %i" % exitCode)
    (exitCode, wasmPath) = translate(compiledPath)

    if exitCode != 0:
      # HACK: If JSILc fails ensure that the C# compile and JSILc compile are repeated next run
      os.unlink(compiledPath)
    self.assertEqual(0, exitCode, "JSILc failed with exit code %i" % exitCode)    

    exitCode = run_csharp(compiledPath)
    self.assertEqual(0, exitCode, "C# test case failed with exit code %i" % exitCode)
    (exitCode, csBytes, wasmBytes) = run_wasm(wasmPath, stdoutPath)

    self.assertEqual(csBytes, wasmBytes, "C# and wasm output did not match (%s)" % stdoutPath)

    if exitCode != 0:
      # HACK: If JSILc fails ensure that the C# compile and JSILc compile are repeated next run
      os.unlink(compiledPath)
    self.assertEqual(0, exitCode, "wasm interpreter failed with exit code %i" % exitCode)

def generate_test_case(fileName):
  return lambda self : self._runTestFile(fileName)


def generate_test_cases(cls, files):
  for fileName in files:
    testCase = generate_test_case(fileName)
    setattr(cls, fileName, testCase)

if __name__ == "__main__":
  try:
    os.makedirs("output/wasm-data")
  except OSError:
    pass

  testFiles = glob.glob(os.path.join("third_party", "tests", "*.cs"))
  unittest.TestLoader.testMethodPrefix = os.path.join("third_party", "tests")
  generate_test_cases(RunTests, testFiles)
  shutil.copy2(os.path.join("WasmMeta", "bin", "WasmMeta.dll"), "output")
  unittest.main()
