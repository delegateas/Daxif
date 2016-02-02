F# Setup
========

In order to execute these F# script, Visual F# Tools must be installed. The tools can be downloaded from:

* [Visual F# Tools 3.1.2][fst1]
* [Visual F# Tools 4.0 Preview][fst2]

[fst1]: https://www.microsoft.com/en-us/download/details.aspx?id=44011
[fst2]: https://www.microsoft.com/en-us/download/details.aspx?id=44941

After installation run the following from a <code>cmd.exe</code> (run as Administrator):

Visual F# Tools 3.1.2 (Windows Explorer right-click "Run with F# Interactive..."):

    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" ^
        /v Path /t REG_SZ /d "%PATH%;C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0"

or Visual F# Tools 4.0 Preview (use this for newer versions of Daxif):

    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" ^
        /v Path /t REG_SZ /d "%PATH%;C:\Program Files (x86)\Microsoft SDKs\F#\4.0\Framework\v4.0"

and then log off/login and the Register Database will be updated (no need for restart)