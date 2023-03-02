# Code Removed from the Project, but still available for reference

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
