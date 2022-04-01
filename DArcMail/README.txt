Installing DArcMail (version 2.0 2021-06-11)
--------------------------------------------

Application Help
----------------
Application help is in the 'help' directory of the distribution package. To start viewing help, open the 'index.html' with a browser.

DArcMail vs. DArcMailXml
-------------------------
DArcMail uses a database. It loads the contents of email account (as contained in one or more .mbox files) in the database. You can then search for and view email messages in the account.

DArcMailXml does not use a database. You can use DArcMailXml only to transform an email account (as contained in one or more .mbox files) into an XML file. DArcMail and DArcMailXml use the same application code base. If you install DArcMail, you have also installed DArcMailXml.

CmdDArcMailXml is a command-line version of DArcMailXml. To install ONLY CmdDArcMailXml, it is not neccessary to install the wxPython module.

Database Implementation
-----------------------
DArcMail uses SQLite as the database (see https://sqlite.org)

SQLite is a file-based database engine. It is packaged with the standard Python distributions. No special installation for SQLite is required beyond having Python3 installed.

Requirements for DArcMail
-------------------------
DArcMail will run on Windows, Mac OSX, and Linux (Ubuntu). It requires Python3 (version 3.6 or later). If you intend to use programs DArcMail or DArcMailXml, you need to have Python3 module wxPython (version 4.0 or later) installed (see https://www.wxpython.org).

Copy the DArcMail Python Files
------------------------------
The DArcMail programs are in the src directory of the distribution. They can be copied to, and run from, anywhere on your computer. You can run them from where you have unzipped the distribution package, or you can copy all the src/*.py files in the distribution to another location.
