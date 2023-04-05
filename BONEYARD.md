# Code Removed from the Project, but still available for reference

## Function to automatically retry opening a file stream with an exponential backoff
        /// <summary>
        /// Try to open a file stream with multiple retries with exponential backoff in case it is locked by some other process
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        /// <param name="share"></param>
        /// <returns></returns>
        public static FileStream? WaitForFile(string fullPath, FileMode mode, FileAccess access, FileShare share)
        {
            for (int numTries = 0; numTries < 10; numTries++)
            {
                FileStream? fs = null;
                try
                {
                    fs = new FileStream(fullPath, mode, access, share);
                    return fs;
                }
                catch (IOException)
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                    Thread.Sleep(50 * 2 ^ numTries); //each loop is given a bit longer
                }
            }

            return null;
        }

## Function to inline css styles using the ExCSS parser

        const int CSS_PARSER_TIMEOUT = 700; //milliseconds
        const int CSS_PARSER_SEVERITY_RUNTIME = 0;


        /// <summary>
        /// Convert any embedded stylesheets into inline styles
        /// </summary>
        /// <param name="htmlNode"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        /// <returns></returns>
        private static void ConvertToInlineCssUsingExCss(HtmlNode htmlNode, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            //get all the style elements
            var styles = htmlNode.SelectNodes("//style");

            string allStyles = "";
            if (styles != null)
            {
                foreach (var style in styles)
                {
                    allStyles += style.InnerText + " ;\r\n/* NEXT STYLE */\r\n";
                }

                //minify the css; this also checks for errors
                var uResult = Uglify.Css(allStyles);
                foreach (var err in uResult.Errors)
                {
                    messages.Add((err.Severity == CSS_PARSER_SEVERITY_RUNTIME ? LogLevel.Warning : LogLevel.Information, $"CSS error: {err.Message}"));
                }
                if (uResult.Errors.Any(e => e.Severity == CSS_PARSER_SEVERITY_RUNTIME)) //the most severe run-time invoking error
                {
                    // do not try to continue 
                    return;
                }

                var cssParser = new StylesheetParser(); //ExCSS parser
                //The parser may get stuck in an endless loop:  https://github.com/TylerBrinks/ExCSS/issues/138
                var sSheetTask = Task.Run(() => cssParser.Parse(allStyles));
                var done = sSheetTask.Wait(CSS_PARSER_TIMEOUT); //milliseconds
                if (!done)
                {
                    messages.Add((LogLevel.Critical, "The ExCSS parser is possibly stuck in an endless loop or a deadlock"));// do not try to continue
                    throw new Exception("The ExCSS parser is possibly stuck in an endless loop; best to just abort");
                }

                foreach (var rule in sSheetTask.Result.StyleRules.OrderByDescending(sr => sr.Selector.Specificity))
                {
                    //TODO: Add support for ListSelectors, so that the separate specificity of its selectors can be determined.
                    //      The ExCss library doesn't seem to allow access to the individual selectors of a SelectorList
                    //      I need to do a pull-request for the ExCSS change that I would like
                    //TODO: Add support for the shorthand property "all"
                    //TODO: Add support for the !important modifier.

                    var selectorText = rule.Selector.Text;
                    var styleText = rule.Style.CssText;
                    var specificity = rule.Selector.Specificity;

                    if (string.IsNullOrWhiteSpace(styleText))
                        continue; //skip this rule andf move to the next

                    IEnumerable<HtmlNode>? nodes = null;
                    try
                    {
                        nodes = htmlNode.QuerySelectorAll(selectorText);
                    }
                    catch (Exception ex)
                    {
                        messages.Add((LogLevel.Debug, $"Parsing selector '{selectorText}'; {ex.Message}"));
                        nodes = null;
                    }

                    if (nodes != null)
                    {
                        foreach (var node in nodes)
                        {
                            var styleAttr = node.GetAttributes("style").SingleOrDefault();
                            if (styleAttr == null)
                            {
                                var newStyle = cssParser.Parse($"dummy {{{styleText}}}").StyleRules.Single().Style;
                                var newStyleText = string.Join("; ", newStyle.Select(p => p.Name + ":" + p.Value)) + "; ";

                                node.Attributes.Add("style", newStyleText);
                                messages.Add((LogLevel.Debug, $"Node: {node.XPath}, Inlining style:  {selectorText} {{{styleText}}}, new style attribute"));
                            }
                            else
                            {
                                var currentStyle = cssParser.Parse($"dummy {{{styleAttr.Value}}}").StyleRules.Single().Style;

                                var currentStyleText = string.Join("; ", currentStyle.Select(p => p.Name + ":" + p.Value)) + "; ";

                                foreach (var newProperty in rule.Style)
                                {
                                    //since we are iterating rules from most specific to the least specific, we can't overwrite an existing property because it originated from a more specific rule
                                    if (!currentStyle.Contains(newProperty, new ExCssPropertyComparer()))
                                    {
                                        currentStyleText += newProperty.Name + ":" + newProperty.Value + "; ";
                                        messages.Add((LogLevel.Debug, $"Node: {node.XPath}, Inlining style: {selectorText} {{{styleText}}}, modify style attribute"));
                                    }
                                }

                                styleAttr.Value = currentStyleText.Trim().Trim(';').Trim(); //get rid of any leading or trainling semi-colons and whitespace

                            }
                        }
                    }
                }
            }
        }


## Function to return a page given a named destination

        private PdfPage? GetPageWithNamedDestination(PdfDictionary dests, string name)
        {

            PdfPage? ret = null;

            PdfArray destNames = dests.GetAsArray(PdfName.Names);
            PdfArray destKids = dests.GetAsArray(PdfName.Kids);
            PdfArray destLimits = dests.GetAsArray(PdfName.Limits);

            string lowerLimit;
            string upperLimit;
            if (destLimits != null)
            {
                lowerLimit = destLimits.GetAsString(0).ToString();
                upperLimit = destLimits.GetAsString(1).ToString();
                int compLower = string.Compare(name, lowerLimit, StringComparison.Ordinal);
                int compUpper = string.Compare(name, upperLimit, StringComparison.Ordinal);
                if (compLower < 0 || compUpper > 0)
                    return null;
            }

            if (destNames != null)
            {
                for (int i = 0; i < destNames.ArrayList.Count; i += 2)
                {
                    var key = destNames.GetAsString(i).ToString();
                    var value = destNames.GetDirectObject(i + 1);
                    if (string.Compare(name, key, StringComparison.Ordinal) == 0)
                    {
                        PdfArray destination;
                        if (value.IsDictionary())
                        {
                            destination= (PdfArray)((PdfDictionary)value).Get(PdfName.D);
                        }
                        else if (value.IsArray())
                        {
                            destination = (PdfArray)value;
                        }
                        else
                        {
                            throw new Exception("Invalid destination name value");
                        }

                        //Get the page from the destination
                        ret = (PdfPage)destination.GetDirectObject(0);
                    }

                }
            }
            else if (destKids != null)
            {
                for (int i = 0; i < destKids.Size; i += 1)
                {
                    PdfObject nodes = (PdfDictionary)destKids.GetDirectObject(i);
                    if (nodes.IsDictionary())
                    {
                        ret = GetPageWithNamedDestination((PdfDictionary)nodes, name);
                        if (ret != null)
                            break;
                    }
                    else
                    {
                        throw new Exception("Invalid kids");
                    }
                }
            }
            else
            {
                throw new Exception("Invalid name tree");
            }


            return ret;
        }
