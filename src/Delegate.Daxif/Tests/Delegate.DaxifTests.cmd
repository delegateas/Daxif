@echo off
:: Add the paths for the F# SDK 3.x or 4.x (from higher version to lower)
set FSHARPSDK=^
C:\Program Files (x86)\Microsoft SDKs\F#\4.0\Framework\v4.0\;^
C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\;^
C:\Program Files (x86)\Microsoft SDKs\F#\3.0\Framework\v4.0\

set A1=%1
set VS_SOLUTIONDIR=%A1:"=%

cls
:: Delete previous "__temp__.fsx" and generate a new one with "SetupUnitTests.fsx"
for %%i in (fsianycpu.exe) do "%%~$FSHARPSDK:i" %VS_SOLUTIONDIR%Delegate.Daxif\Tests\SetupTests.fsx %*

:: Execute the script "only" with the first "fsianycpu.exe" found
for %%i in (fsianycpu.exe) do "%%~$FSHARPSDK:i" %VS_SOLUTIONDIR%Delegate.Daxif\Tests\Delegate.DaxifTests.fsx %*

:: No test due to we can't have a DEV environment :(