# _Dev Tools_
Logging in Unit Tests:
https://codeburst.io/unit-testing-with-net-core-ilogger-t-e8c16c503a80

Getting a Linux version of the application
https://www.codeproject.com/Tips/5255423/Linux-on-Windows

# _Conversion Notes_
If the the RenderX XEP processor is used, attached files must be base64 encoded and if external, wrapped in XML.  XEP will fail when attaching a slightly malformed PDF file.
By wrapping them, we can trick the processor inot treating them as arbitrary binary attachments which it can handle just fine.