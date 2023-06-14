<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE stylesheet
[
<!ENTITY mdash "&#8212;" >
<!ENTITY nbsp "&#160;" >
]>

<xsl:stylesheet version="2.0" 
	xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"

	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:fo="http://www.w3.org/1999/XSL/Format"
	xmlns:html="http://www.w3.org/1999/xhtml"
	xmlns:xs="http://www.w3.org/2001/XMLSchema"
	xmlns:fn="http://www.w3.org/2005/xpath-functions"
	xmlns:my="http://library.illinois.edu/myFunctions"
	
	xmlns:pdf="http://xmlgraphics.apache.org/fop/extensions/pdf"
	xmlns:fox="http://xmlgraphics.apache.org/fop/extensions"
	
	xmlns:rx="http://www.renderx.com/XSL/Extensions"
	>

	<xsl:import href="eaxs_xhtml2fo.xsl"/>

	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="no" omit-xml-declaration="no" cdata-section-elements="rx:custom-meta"/>
	
	<!-- The font-family values to use for serif, sans-serif, and monospace fonts repsectively -->
	<xsl:param name="SerifFont" select="'Times'"/>
	<xsl:param name="SansSerifFont" select="'Helvetica'"/>
	<xsl:param name="MonospaceFont" select="'Courier'"/>
	<xsl:variable name="DefaultFont" select="$SerifFont"/><!-- Used as the default at the root level of the document, and anywhere else that needs a font which can't be definitely determined -->
	
	<xsl:param name="icc-profile" select="'file:/C:/Program Files/RenderX/XEP/sRGB2014.icc'"/>

	<!-- TGH Which FO Processor -->
	<xsl:param name="fo-processor-version">FOP Version 2.8</xsl:param> <!-- Values used: fop or xep -->
	<xsl:variable name="fo-processor" select="fn:lower-case(fn:tokenize($fo-processor-version)[1])"/>
	<xsl:variable name="producer">UIUCLibrary.EaPdf; <xsl:value-of select="$fo-processor-version"/></xsl:variable>
	
	<!-- Applied to FOP processor; if true FOP will use JavaScript to embed link to open the attachment; JavaScript is not allowed in PDF/A files -->
	<xsl:param name="use-embedded-file-link">false</xsl:param>
	
	<xsl:param name="generate-xmp">false</xsl:param><!-- generate the XMP metadatat -->
	
	<xsl:template name="check-params">
		<xsl:choose>
			<xsl:when test="$fo-processor='fop'"/>
			<xsl:when test="$fo-processor='xep'"/>
			<xsl:otherwise>
				<xsl:message terminate="yes">
					The value '<xsl:value-of select="$fo-processor"/>' is not a valid value for fo-processor param; the only allowed values are 'fop' and 'xep'.
				</xsl:message>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="/">
		
		<xsl:call-template name="check-params"/>
		
		<xsl:if test="$fo-processor='xep'">
			<!-- Add ICC color profile -->
			<xsl:processing-instruction name="xep-pdf-icc-profile">url(<xsl:value-of select="$icc-profile"/>)</xsl:processing-instruction>
		</xsl:if>
		
		<fo:root xml:lang="en">
			<xsl:attribute name="font-family"><xsl:value-of select="$DefaultFont"/></xsl:attribute>
			<xsl:if test="$fo-processor='xep'">
				<xsl:call-template name="xep-metadata"/>
			</xsl:if>
			<fo:layout-master-set>
				<fo:simple-page-master master-name="message-page" page-width="8.5in"
					page-height="11in">
					<fo:region-body margin-top="1in" margin-bottom="1in" margin-left="1in"
						margin-right="1in"/>
					<fo:region-before extent="1in"/>
					<fo:region-after extent="1in"/>
				</fo:simple-page-master>
			</fo:layout-master-set>
			
			<xsl:call-template name="declarations"/>
			
			<xsl:call-template name="bookmarks"/>
			
			<fo:page-sequence master-reference="message-page">
				<fo:static-content flow-name="xsl-region-before">
					<fo:block text-align="center" margin-left="1in" margin-right="1in">Account:
							<xsl:value-of select="/eaxs:Account/eaxs:EmailAddress[1]"/> Folder:
							<xsl:value-of select="/eaxs:Account/eaxs:Folder/eaxs:Name"/></fo:block>
				</fo:static-content>
				<fo:static-content flow-name="xsl-region-after">
					<fo:block text-align="center" margin-left="1in" margin-right="1in">
						<fo:page-number/>
					</fo:block>
				</fo:static-content>
				<fo:flow flow-name="xsl-region-body">
					<xsl:call-template name="CoverPage"/>
					<xsl:apply-templates select="/eaxs:Account/eaxs:Folder[eaxs:Message]" mode="RenderContent"/>
					<xsl:call-template name="AttachmentsList"/>
				</fo:flow>
				<!-- TODO: Add a section which is the conversion report with info, warning, and error messages -->
				<!--       Might need to embedd these into the source XML, not just as xml comments -->
			</fo:page-sequence>
		</fo:root>
	</xsl:template>
	
	<xsl:template name="xep-metadata">
		<xsl:if test="fn:lower-case(normalize-space($generate-xmp)) = 'true'">
			<rx:meta-info>
				<rx:custom-meta>
					<xsl:variable name="xmp"><xsl:call-template name="xmp"></xsl:call-template></xsl:variable>
					<xsl:value-of select="fn:serialize($xmp)"/>
				</rx:custom-meta>
				<!-- other custom metadata fields -->
				<!-- <rx:meta-field name="NAME_XEP" value="VALUE_XEP"/> -->
			</rx:meta-info>
		</xsl:if>
	</xsl:template>
	
	<xsl:template name="AttachmentsList">
		<fo:block id="AttachmentList" xsl:use-attribute-sets="h1"><fox:destination internal-destination="AttachmentList"/>All Attachments</fo:block>
		<fo:block background-color="beige" border="1px solid brown" padding="0.125em">
			You may need to open the PDF reader's attachments list to download or open these files. Look for the name that matches the long random-looking string of characters.
		</fo:block>
		<fo:block xsl:use-attribute-sets="h2">Source Email Files</fo:block>
		<fo:block xsl:use-attribute-sets="dl">
			<!-- Source MBOX files are referenced in FolderProperties -->
			<xsl:for-each select="//eaxs:Folder[eaxs:Message]/eaxs:FolderProperties[eaxs:RelPath]">
				<fo:block xsl:use-attribute-sets="dt" font-weight="bold">
					<xsl:attribute name="id">SRC_<xsl:value-of select="eaxs:Hash/eaxs:Value"/></xsl:attribute>
					<fox:destination><xsl:attribute name="internal-destination">SRC_<xsl:value-of select="eaxs:Hash/eaxs:Value"/></xsl:attribute></fox:destination>
					<xsl:value-of select="../eaxs:Name"/>
				</fo:block>
				<fo:block xsl:use-attribute-sets="dd">
					<xsl:value-of select="eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="eaxs:FileExt"/>
				</fo:block>
			</xsl:for-each>
			<!-- Source EML files are referenced in MessageProperties -->
			<xsl:for-each select="//eaxs:Folder/eaxs:Message/eaxs:MessageProperties[eaxs:RelPath]">
				<fo:block xsl:use-attribute-sets="dt" font-weight="bold">
					<xsl:attribute name="id">SRC_<xsl:value-of select="eaxs:Hash/eaxs:Value"/></xsl:attribute>
					<fox:destination><xsl:attribute name="internal-destination">SRC_<xsl:value-of select="eaxs:Hash/eaxs:Value"/></xsl:attribute></fox:destination>
					<xsl:value-of select="../eaxs:MessageId"/>
				</fo:block>
				<fo:block xsl:use-attribute-sets="dd">
					<xsl:value-of select="eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="eaxs:FileExt"/>
				</fo:block>
			</xsl:for-each>
		</fo:block>

		<fo:block xsl:use-attribute-sets="h2">File Attachments</fo:block>
		<fo:block xsl:use-attribute-sets="dl">
			<xsl:for-each select="//eaxs:SingleBody/eaxs:ExtBodyContent | //eaxs:SingleBody/eaxs:BodyContent[fn:lower-case(normalize-space(../@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(../eaxs:ContentType)),'text/'))]">
				<fo:block xsl:use-attribute-sets="dt"  font-weight="bold">
					<xsl:attribute name="id">ATT_<xsl:value-of select="eaxs:Hash/eaxs:Value"/></xsl:attribute>
					<fox:destination><xsl:attribute name="internal-destination">ATT_<xsl:value-of select="eaxs:Hash/eaxs:Value"/></xsl:attribute></fox:destination>
					<xsl:value-of select="../eaxs:DispositionFile | ../eaxs:ContentName"/>
					<xsl:if test="eaxs:Size">
						<fo:inline font-size="small"> (<xsl:value-of select="fn:format-number(eaxs:Size,'0,000')"/> bytes)</fo:inline>
					</xsl:if>
				</fo:block>
				<fo:block xsl:use-attribute-sets="dd">
					<xsl:value-of select="eaxs:Hash/eaxs:Value"/>.<xsl:call-template name="GetFileExtension"/>
				</fo:block>
			</xsl:for-each>
		</fo:block>
	</xsl:template>
	
	<xsl:template name="bookmarks">
		<fo:bookmark-tree>
			<xsl:apply-templates select="/eaxs:Account/eaxs:Folder[eaxs:Message]" mode="RenderBookmarks"></xsl:apply-templates>
		</fo:bookmark-tree>
	</xsl:template>
	
	<xsl:template match="eaxs:Folder" mode="RenderBookmarks">
		<xsl:param name="topfolder">true</xsl:param>
		<xsl:if test="$topfolder='true'">
			<fo:bookmark>
				<xsl:attribute name="internal-destination">CoverPage</xsl:attribute>
				<fo:bookmark-title>Cover Page</fo:bookmark-title>
			</fo:bookmark>
		</xsl:if>
		<fo:bookmark>
			<xsl:attribute name="internal-destination"><xsl:value-of select="generate-id(.)"/></xsl:attribute>
			<fo:bookmark-title><xsl:value-of select="eaxs:Name"/></fo:bookmark-title>
			<fo:bookmark starting-state="hide">
				<xsl:attribute name="internal-destination"><xsl:value-of select="generate-id(.)"/></xsl:attribute>
				<fo:bookmark-title><xsl:value-of select="count(eaxs:Message)"/> Messages</fo:bookmark-title>
				<xsl:apply-templates select="eaxs:Message" mode="RenderBookmarks"/>
			</fo:bookmark>
			<xsl:if test="eaxs:Folder[eaxs:Message]">
				<fo:bookmark starting-state="hide">
					<xsl:attribute name="internal-destination"><xsl:value-of select="generate-id(.)"/></xsl:attribute>
					<fo:bookmark-title><xsl:value-of select="count(eaxs:Folder[eaxs:Message])"/> Sub-folders</fo:bookmark-title>
					<xsl:apply-templates select="eaxs:Folder[eaxs:Message]" mode="RenderBookmarks"><xsl:with-param name="topfolder">false</xsl:with-param></xsl:apply-templates>
				</fo:bookmark>
			</xsl:if>
		</fo:bookmark>
		<xsl:if test="$topfolder='true'">
			<fo:bookmark>
				<xsl:attribute name="internal-destination">AttachmentList</xsl:attribute>
				<fo:bookmark-title>List of All Attachments</fo:bookmark-title>
			</fo:bookmark>
		</xsl:if>
	</xsl:template>
	
	<xsl:template match="eaxs:Message" mode="RenderBookmarks">
		<fo:bookmark><xsl:attribute name="internal-destination">MESSAGE_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute>
			<fo:bookmark-title>
				<xsl:value-of select="normalize-space(eaxs:Subject)"/>
				<xsl:text>&#13;&#10;from </xsl:text><xsl:value-of select="normalize-space(eaxs:From)"/>
				<xsl:text>&#13;&#10;on </xsl:text><xsl:value-of select="fn:format-dateTime(eaxs:OrigDate, '[FNn], [MNn] [D], [Y]')"/>
			</fo:bookmark-title>
		</fo:bookmark>			
	</xsl:template>
	
	<xsl:template name="declarations">
		<xsl:if test="$fo-processor='fop'">
			<fo:declarations>
				<xsl:call-template name="declarations-attachments"/>
				<xsl:if test="fn:lower-case(normalize-space($generate-xmp)) = 'true'">
					<xsl:call-template name="xmp"/>
					<!-- other custom metadata fields -->
					<!-- <pdf:info>
						<pdf:name key="NAME_FOP">VALUE_FOP</pdf:name>
					</pdf:info> -->
				</xsl:if>
			</fo:declarations>
		</xsl:if>
	</xsl:template>
	
	<xsl:template name="xmp">
		<!-- NOTE:  This is actually replaced during a post-processing step in the EAXS to PDF processing step -->
		
		<!-- get current date time, converted to Zulu time, and formatted as ISO 8601 without fractional seconds -->
		<xsl:variable name="datetime-string" select="fn:format-dateTime(fn:adjust-dateTime-to-timezone(fn:current-dateTime(),xs:dayTimeDuration('P0D')),'[Y0001]-[M01]-[D01]T[H01]:[m01]:[s01]Z')"/>
		
		<x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="Adobe XMP Core 9.0-c000 79.cca54b0, 2022/11/26-09:29:55        ">
			<rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
				
				<xsl:if test="$fo-processor='xep'"><!-- This is required by PDF/A, but FOP can't seem to deal with extension schema, so will probably need to add these during post processing -->
					<rdf:Description rdf:about=""
						xmlns:pdfaExtension="http://www.aiim.org/pdfa/ns/extension/"
						xmlns:pdfaSchema="http://www.aiim.org/pdfa/ns/schema#"
						xmlns:pdfaProperty="http://www.aiim.org/pdfa/ns/property#"
						>
						<pdfaExtension:schemas>
							<rdf:Bag>
								<rdf:li>
									<rdf:Description 
										xmlns:pdfaExtension="http://www.aiim.org/pdfa/ns/extension/"
										xmlns:pdfaSchema="http://www.aiim.org/pdfa/ns/schema#"
										xmlns:pdfaProperty="http://www.aiim.org/pdfa/ns/property#"
										>
										<pdfaSchema:schema>Archival PDF (PDF/A) Profile Identification schema</pdfaSchema:schema>
										<pdfaSchema:namespaceURI>http://www.aiim.org/pdfa/ns/id/</pdfaSchema:namespaceURI>
										<pdfaSchema:prefix>pdfaid</pdfaSchema:prefix>
										<pdfaSchema:property>
											<rdf:Seq>
												<rdf:li>
													<rdf:Description 
														xmlns:pdfaExtension="http://www.aiim.org/pdfa/ns/extension/"
														xmlns:pdfaSchema="http://www.aiim.org/pdfa/ns/schema#"
														xmlns:pdfaProperty="http://www.aiim.org/pdfa/ns/property#"
														>
														<pdfaProperty:category>internal</pdfaProperty:category>
														<pdfaProperty:description>The PDF/A conformance level, a, b, u, to which the document complies.</pdfaProperty:description>
														<pdfaProperty:name>conformance</pdfaProperty:name>
														<pdfaProperty:valueType>Text</pdfaProperty:valueType>
													</rdf:Description>
												</rdf:li>
												<rdf:li>
													<rdf:Description 
														xmlns:pdfaExtension="http://www.aiim.org/pdfa/ns/extension/"
														xmlns:pdfaSchema="http://www.aiim.org/pdfa/ns/schema#"
														xmlns:pdfaProperty="http://www.aiim.org/pdfa/ns/property#"
														>
														<pdfaProperty:category>internal</pdfaProperty:category>
														<pdfaProperty:description>The part of the PDF/A profile to which the document complies.</pdfaProperty:description>
														<pdfaProperty:name>part</pdfaProperty:name>
														<pdfaProperty:valueType>Text</pdfaProperty:valueType>
													</rdf:Description>
												</rdf:li>
											</rdf:Seq>
										</pdfaSchema:property>
									</rdf:Description>
								</rdf:li>
								<rdf:li>
									<rdf:Description  
										xmlns:pdfaExtension="http://www.aiim.org/pdfa/ns/extension/"
										xmlns:pdfaSchema="http://www.aiim.org/pdfa/ns/schema#"
										xmlns:pdfaProperty="http://www.aiim.org/pdfa/ns/property#"
										>
										<pdfaSchema:schema>EA PDF (PDF/mail) Profile Identification schema</pdfaSchema:schema>
										<pdfaSchema:namespaceURI>http://www.pdfa.org/eapdf/ns/id/</pdfaSchema:namespaceURI>
										<pdfaSchema:prefix>pdfmailid</pdfaSchema:prefix>
										<pdfaSchema:property>
											<rdf:Seq>
												<rdf:li>
													<rdf:Description 
														xmlns:pdfaExtension="http://www.aiim.org/pdfa/ns/extension/"
														xmlns:pdfaSchema="http://www.aiim.org/pdfa/ns/schema#"
														xmlns:pdfaProperty="http://www.aiim.org/pdfa/ns/property#"
														>
														<pdfaProperty:category>internal</pdfaProperty:category>
														<pdfaProperty:description>The PDF/mail conformance level, s, m, or c, to which the document complies.</pdfaProperty:description>
														<pdfaProperty:name>conformance</pdfaProperty:name>
														<pdfaProperty:valueType>Text</pdfaProperty:valueType>
													</rdf:Description>
												</rdf:li>
												<rdf:li>
													<rdf:Description 
														xmlns:pdfaExtension="http://www.aiim.org/pdfa/ns/extension/"
														xmlns:pdfaSchema="http://www.aiim.org/pdfa/ns/schema#"
														xmlns:pdfaProperty="http://www.aiim.org/pdfa/ns/property#"
														>
														<pdfaProperty:category>internal</pdfaProperty:category>
														<pdfaProperty:description>The part of the PDF/mail profile to which the document complies.</pdfaProperty:description>
														<pdfaProperty:name>part</pdfaProperty:name>
														<pdfaProperty:valueType>Text</pdfaProperty:valueType>
													</rdf:Description>
												</rdf:li>
												<rdf:li>
													<rdf:Description 
														xmlns:pdfaExtension="http://www.aiim.org/pdfa/ns/extension/"
														xmlns:pdfaSchema="http://www.aiim.org/pdfa/ns/schema#"
														xmlns:pdfaProperty="http://www.aiim.org/pdfa/ns/property#"
														>
														<pdfaProperty:category>internal</pdfaProperty:category>
														<pdfaProperty:description>The revision of the PDF/mail profile to which the document complies.</pdfaProperty:description>
														<pdfaProperty:name>rev</pdfaProperty:name>
														<pdfaProperty:valueType>Text</pdfaProperty:valueType>
													</rdf:Description>
												</rdf:li>
											</rdf:Seq>
										</pdfaSchema:property>
									</rdf:Description>
								</rdf:li>
							</rdf:Bag>
						</pdfaExtension:schemas>
					</rdf:Description>
					
				</xsl:if>
				
				<rdf:Description rdf:about=""
					xmlns:dc="http://purl.org/dc/elements/1.1/"
					
					xmlns:pdf="http://ns.adobe.com/pdf/1.3/"
					xmlns:pdfx="http://ns.adobe.com/pdfx/1.3/"
					xmlns:xmp="http://ns.adobe.com/xap/1.0/"
					
					xmlns:pdfaid="http://www.aiim.org/pdfa/ns/id/"
					
					xmlns:pdfmail="http://www.pdfa.org/eapdf/"
					xmlns:pdfmailid="http://www.pdfa.org/eapdf/ns/id/"
					
					>
					
					<dc:description>
						<rdf:Alt>
							<rdf:li xml:lang="en">
								<xsl:text>PDF Email Archive</xsl:text> 
								<xsl:if test="/eaxs:Account/eaxs:EmailAddress">
									<xsl:text> for Account '</xsl:text><xsl:value-of select="fn:string-join(/eaxs:Account/eaxs:EmailAddress,', ')"/><xsl:text>'</xsl:text>
								</xsl:if> 
								<xsl:if test="/eaxs:Account/eaxs:Folder[eaxs:Message]">
									<xsl:text> for Folder '</xsl:text><xsl:value-of select="/eaxs:Account/eaxs:Folder[eaxs:Message]/eaxs:Name"/><xsl:text>'</xsl:text>							
								</xsl:if>								
							</rdf:li>
						</rdf:Alt>
					</dc:description>
					
					<dc:identifier><xsl:value-of select="/eaxs:Account/eaxs:GlobalId"/></dc:identifier>
					
					<dc:format>application/pdf</dc:format>
					<dc:language>
						<rdf:Bag>
							<rdf:li>x-unknown</rdf:li>
						</rdf:Bag>
					</dc:language>
					<dc:date>
						<rdf:Seq>
							<rdf:li><xsl:value-of select="$datetime-string"/></rdf:li>
						</rdf:Seq>
					</dc:date>
					
					<pdf:Producer><xsl:value-of select="$producer"/></pdf:Producer>
					<pdf:PDFVersion>1.7</pdf:PDFVersion>
					
					<pdfaid:part>3</pdfaid:part>
					<pdfaid:conformance>U</pdfaid:conformance>
					
					<pdfmailid:part>1</pdfmailid:part>
					<pdfmailid:rev>2022</pdfmailid:rev>
					<pdfmailid:conformance>m</pdfmailid:conformance>
					
					<xmp:CreatorTool>UIUCLibrary.EaPdf</xmp:CreatorTool>
					<xmp:MetadataDate><xsl:value-of select="$datetime-string"/></xmp:MetadataDate>
					<xmp:CreateDate><xsl:value-of select="$datetime-string"/></xmp:CreateDate>
					<xmp:ModifyDate><xsl:value-of select="$datetime-string"/></xmp:ModifyDate>
				</rdf:Description>
				
			</rdf:RDF>
		</x:xmpmeta>
	</xsl:template>
	
	<xsl:template name="declarations-attachments">
		<!-- if the source is an MBOX file, it is referenced in Folder/FolderProperties -->
		<xsl:for-each select="//eaxs:Folder[eaxs:Message]/eaxs:FolderProperties[eaxs:RelPath]">
			<pdf:embedded-file>
				<xsl:attribute name="filename"><xsl:value-of select="eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="eaxs:FileExt"/></xsl:attribute>
				<xsl:attribute name="description">Source file for mail folder '<xsl:value-of select="../eaxs:Name"/>'</xsl:attribute>
				<xsl:attribute name="src">url(<xsl:value-of select="fn:resolve-uri(eaxs:RelPath, fn:base-uri())"/>)</xsl:attribute>
			</pdf:embedded-file>				
		</xsl:for-each>
		<!-- if the source is an EML file, it is referenced in Folder/Message/MessageProperties -->
		<xsl:for-each select="//eaxs:Folder/eaxs:Message/eaxs:MessageProperties[eaxs:RelPath]">
			<pdf:embedded-file>
				<xsl:attribute name="filename"><xsl:value-of select="eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="eaxs:FileExt"/></xsl:attribute>
				<xsl:attribute name="description">Source file for message '<xsl:value-of select="../eaxs:MessageId"/>'</xsl:attribute>
				<xsl:attribute name="src">url(<xsl:value-of select="fn:resolve-uri(eaxs:RelPath, fn:base-uri())"/>)</xsl:attribute>
			</pdf:embedded-file>				
		</xsl:for-each>
		<!-- inline attachments which are not text -->
		<xsl:for-each select="//eaxs:SingleBody/eaxs:BodyContent[fn:lower-case(normalize-space(../@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(../eaxs:ContentType)),'text/'))]">
			<xsl:variable name="filename">
				<xsl:value-of select="eaxs:Hash/eaxs:Value"/><xsl:text>.</xsl:text><xsl:call-template name="GetFileExtension"/>
			</xsl:variable>
			<pdf:embedded-file>
				<xsl:attribute name="filename"><xsl:value-of select="$filename"/></xsl:attribute>
				<xsl:attribute name="description">Original File Name: <xsl:value-of select="../eaxs:DispositionFile | ../eaxs:ContentName"/></xsl:attribute>
				<xsl:attribute name="src">
					<xsl:call-template name="data-uri">
						<xsl:with-param name="content-type" select="../eaxs:ContentType"/>
						<xsl:with-param name="transfer-encoding" select="eaxs:TransferEncoding"/>
						<xsl:with-param name="content" select="eaxs:Content"/>
					</xsl:call-template>
				</xsl:attribute>
			</pdf:embedded-file>								
		</xsl:for-each>
		<!-- external attachments -->
		<xsl:for-each select="//eaxs:SingleBody/eaxs:ExtBodyContent">
			<xsl:variable name="rel-path">
				<xsl:call-template name="concat-path">
					<xsl:with-param name="path1" select="normalize-space(ancestor::eaxs:Message/eaxs:RelPath)"/>
					<xsl:with-param name="path2" select="normalize-space(eaxs:RelPath)"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:variable name="filename">
				<xsl:value-of select="eaxs:Hash/eaxs:Value"/><xsl:text>.</xsl:text><xsl:call-template name="GetFileExtension"/>
			</xsl:variable>
			<pdf:embedded-file>
				<xsl:attribute name="filename"><xsl:value-of select="$filename"/></xsl:attribute>
				<xsl:attribute name="description">Original File Name: <xsl:value-of select="../eaxs:DispositionFile | ../eaxs:ContentName"/></xsl:attribute>
				<xsl:choose>
					<xsl:when test="fn:normalize-space(fn:lower-case(eaxs:XMLWrapped)) = 'false'">
						<xsl:attribute name="src">url(<xsl:value-of select="fn:resolve-uri($rel-path, fn:base-uri())"/>)</xsl:attribute>								
					</xsl:when>
					<xsl:when test="fn:normalize-space(fn:lower-case(eaxs:XMLWrapped)) = 'true'">
						<xsl:attribute name="src">
							<xsl:call-template name="data-uri">
								<xsl:with-param name="transfer-encoding" select="document(fn:resolve-uri($rel-path, fn:base-uri()))/eaxs:BodyContent/eaxs:TransferEncoding"/>
								<xsl:with-param name="content-type" select="../eaxs:ContentType"/>
								<xsl:with-param name="content" select="document(fn:resolve-uri($rel-path, fn:base-uri()))/eaxs:BodyContent/eaxs:Content"/>
							</xsl:call-template>
						</xsl:attribute>																
					</xsl:when>
				</xsl:choose>
			</pdf:embedded-file>								
		</xsl:for-each>		
	</xsl:template>
	
	<xsl:template name="CoverPage">
		<fo:block id="CoverPage" page-break-after="always">
			<fox:destination internal-destination="CoverPage"/>
			<fo:block xsl:use-attribute-sets="h1">PDF Email Archive (PDF/mail-1m)</fo:block>
			<fo:block xsl:use-attribute-sets="h2">
				<fo:block>
					<xsl:text>Created On: </xsl:text>
					<xsl:value-of select="fn:format-dateTime(fn:current-dateTime(), '[FNn], [MNn] [D], [Y], [h]:[m]:[s] [PN]')"/>
				</fo:block>
				<fo:block>
					<xsl:text>Created By: </xsl:text>
					<xsl:value-of select="$producer"/>
				</fo:block>
			</fo:block>
			<xsl:choose>
				<xsl:when test="count(/eaxs:Account/eaxs:EmailAddress) > 1">
					<fo:block xsl:use-attribute-sets="h2">For Account:</fo:block>
					<fo:list-block>
						<xsl:for-each select="/eaxs:Account/eaxs:EmailAddress">
							<fo:list-item>
								<fo:list-item-label end-indent="label-end()"><fo:block>&#x2022;</fo:block></fo:list-item-label>
								<fo:list-item-body start-indent="body-start()"><fo:block><xsl:apply-templates/></fo:block></fo:list-item-body>
							</fo:list-item>
						</xsl:for-each>
					</fo:list-block>
				</xsl:when>
				<xsl:when test="count(/eaxs:Account/eaxs:EmailAddress) = 1">
					<fo:block xsl:use-attribute-sets="h2">For Account: <xsl:value-of select="/eaxs:Account/eaxs:EmailAddress"/></fo:block> 					
				</xsl:when>
			</xsl:choose>
			<fo:block xsl:use-attribute-sets="h2">Global Id: <xsl:value-of select="/eaxs:Account/eaxs:GlobalId"/></fo:block>
			
			<!-- QUESTION: Do not count child messages? -->
			<fo:block xsl:use-attribute-sets="h2">Message Count: <xsl:value-of select="count(//eaxs:Message)"/></fo:block>
			<!-- QUESTION: Only count distinct attachments, based on the hash? -->
			<fo:block xsl:use-attribute-sets="h2">Attachment Count: <xsl:value-of select="count(//eaxs:SingleBody[(eaxs:ExtBodyContent or fn:lower-case(normalize-space(eaxs:BodyContent/eaxs:TransferEncoding)) = 'base64') and (fn:lower-case(normalize-space(@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(eaxs:ContentType)),'text/')))])"/></fo:block>
			<fo:block xsl:use-attribute-sets="h2">Folders: <xsl:value-of select="count(/eaxs:Account//eaxs:Folder[eaxs:Message])"/></fo:block>
			<xsl:apply-templates select="/eaxs:Account/eaxs:Folder[eaxs:Message]" mode="RenderToc"/>
		</fo:block>
	</xsl:template>
	
	<!-- TODO: Need to handle the case where the source is multiple EML files -->
	<xsl:template match="eaxs:Folder" mode="RenderToc">
		<fo:list-block>
			<fo:list-item>
				<fo:list-item-label end-indent="label-end()"><fo:block xsl:use-attribute-sets="h3">&#x2022;</fo:block></fo:list-item-label>
				<fo:list-item-body start-indent="body-start()">
					<fo:block xsl:use-attribute-sets="h3">
						<xsl:apply-templates select="eaxs:Name"/>
						<fo:inline font-size="small"> (<xsl:value-of select="count(eaxs:Message)"/> Messages)</fo:inline>
						<xsl:choose>
							<xsl:when test="$fo-processor='fop'">
								<xsl:for-each select="eaxs:FolderProperties[eaxs:RelPath] | eaxs:Message[1]/eaxs:MessageProperties[eaxs:RelPath]">
									<xsl:choose>
										<xsl:when test="fn:lower-case(normalize-space($use-embedded-file-link))='true'">
											<fo:basic-link>
												<xsl:attribute name="external-destination">url(embedded-file:<xsl:value-of select="eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="eaxs:FileExt"/>)</xsl:attribute>
												<fo:inline font-size="small"> (<fo:inline xsl:use-attribute-sets="a-link" >Open Source File</fo:inline>)</fo:inline>
											</fo:basic-link>
										</xsl:when>
										<xsl:otherwise>
											<fo:basic-link>
												<xsl:attribute name="internal-destination">SRC_<xsl:value-of select="eaxs:Hash/eaxs:Value"/></xsl:attribute>
												<fo:inline>&nbsp;</fo:inline><fo:inline xsl:use-attribute-sets="a-link" font-size="small">Go To Source</fo:inline>
											</fo:basic-link>
										</xsl:otherwise>
									</xsl:choose>
								</xsl:for-each>
							</xsl:when>
							<xsl:when test="$fo-processor='xep'">
								<fo:inline font-size="small">
									<xsl:text>(Open Source File&nbsp;</xsl:text>
									<rx:pdf-comment>
										<xsl:attribute name="title">Source File &mdash; <xsl:value-of select="eaxs:FolderProperties/eaxs:RelPath"/></xsl:attribute>
										<rx:pdf-file-attachment icon-type="paperclip">
											<xsl:attribute name="filename"><xsl:value-of select="eaxs:FolderProperties/eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="eaxs:FolderProperties/eaxs:FileExt"/></xsl:attribute>
											<xsl:attribute name="src">url(<xsl:value-of select="fn:resolve-uri(eaxs:FolderProperties/eaxs:RelPath, fn:base-uri())"/>)</xsl:attribute>
										</rx:pdf-file-attachment>
									</rx:pdf-comment>
									<xsl:text>&nbsp;&nbsp;&nbsp;&nbsp;)</xsl:text>
								</fo:inline>
							</xsl:when>
						</xsl:choose>
						<fo:basic-link>
							<xsl:attribute name="internal-destination"><xsl:value-of select="generate-id(.)"/></xsl:attribute>
							<fo:inline>&nbsp;</fo:inline><fo:inline xsl:use-attribute-sets="a-link" font-size="small">Go To First Message</fo:inline>
						</fo:basic-link>
					</fo:block>
					<xsl:apply-templates select="eaxs:Folder[eaxs:Message]" mode="RenderToc"/>
				</fo:list-item-body>
			</fo:list-item>
		</fo:list-block>
	</xsl:template>
	
	<xsl:template match="eaxs:Folder" mode="RenderContent">
		<fo:block>
			<xsl:attribute name="id"><xsl:value-of select="generate-id(.)"/></xsl:attribute>
			<fox:destination><xsl:attribute name="internal-destination"><xsl:value-of select="generate-id(.)"/></xsl:attribute></fox:destination>
			<xsl:apply-templates select="eaxs:Message" />	
			<xsl:apply-templates select="eaxs:Folder" mode="RenderContent"/>	
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:Message">
		<fo:block page-break-after="always">
			<xsl:attribute name="id">MESSAGE_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute>
			<fox:destination><xsl:attribute name="internal-destination">MESSAGE_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute></fox:destination>
			<fo:block xsl:use-attribute-sets="h3" padding="0.25em" border="1.5pt solid black"><xsl:call-template name="FolderHeader"/> &gt; Message <xsl:value-of select="eaxs:LocalId"/></fo:block>
			<xsl:call-template name="MessageHeaderTocAndContent"/>
			<!-- Create named destination to the end of the message -->
			<fo:inline><xsl:attribute name="id">MESSAGE_END_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute><fox:destination><xsl:attribute name="internal-destination">MESSAGE_END_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute></fox:destination></fo:inline>
		</fo:block>
	</xsl:template>
	
	<xsl:template name="MessageHeaderTocAndContent">
		<xsl:param name="RenderToc">true</xsl:param>
		<fo:list-block provisional-distance-between-starts="6em" provisional-label-separation="0.25em">
			<xsl:apply-templates select="eaxs:MessageId"/>
			<xsl:apply-templates select="eaxs:OrigDate[not('0001-01-01T00:00:00Z')]"/> <!-- MimeKit seems to use this as the default value if there is no date -->
			<xsl:apply-templates select="eaxs:From"/>
			<xsl:apply-templates select="eaxs:Sender"/>
			<xsl:apply-templates select="eaxs:To"/>
			<xsl:apply-templates select="eaxs:Cc"/>
			<xsl:apply-templates select="eaxs:Bcc"/>
			<xsl:apply-templates select="eaxs:Subject"/>
			<xsl:apply-templates select="eaxs:Comments"/>
			<xsl:apply-templates select="eaxs:Keywords"/>
			<xsl:apply-templates select="eaxs:InReplyTo"/>
		</fo:list-block>	
		<xsl:if test="$RenderToc='true'">
			<fo:block xsl:use-attribute-sets="h3" border-bottom="1.5pt solid black">Message Contents</fo:block>
			<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody" mode="RenderToc"/>			
		</xsl:if>
		<xsl:call-template name="hr"/>
		<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody" mode="RenderContent"/>
	</xsl:template>
	
	<xsl:template name="FolderHeader">
		<xsl:for-each select="ancestor::eaxs:Folder">
			<xsl:sort select="position()" data-type="number" order="ascending"/> 
			<xsl:if test="name()='Folder'">
				<xsl:value-of select="eaxs:Name"/>
				<xsl:if test="position() != last()"><xsl:text> &gt; </xsl:text></xsl:if>
			</xsl:if>
		</xsl:for-each>
	</xsl:template>
	
	<xsl:template match="eaxs:MessageId">
		<fo:list-item >
			<fo:list-item-label end-indent="label-end()" >
				<fo:block>Message Id:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates/>									
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:OrigDate">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block>Date:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:value-of select="fn:format-dateTime(., '[FNn], [MNn] [D], [Y], [h]:[m]:[s] [PN]')"/>									
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:From">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block>From:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:Sender">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block>Sender:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:call-template name="mailbox"/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:To">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block>To:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>
	
	<xsl:template match="eaxs:Cc">
		<fo:list-item>
			<fo:list-item-label  end-indent="label-end()">
				<fo:block>CC:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>
	
	<xsl:template match="eaxs:Bcc">
		<fo:list-item>
			<fo:list-item-label  end-indent="label-end()">
				<fo:block>BCC:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:Subject">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block>Subject:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:Comments">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block>Comments:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:Keywords">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block>Keywords:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:InReplyTo">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block>In Reply To:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:Mailbox">
		<xsl:call-template name="mailbox"/>
		<xsl:if test="position() != last()">, </xsl:if>
	</xsl:template>

	<xsl:template match="eaxs:Group">
		<fo:inline font-weight="bold"><xsl:value-of select="eaxs:Name"/> [</fo:inline>
		<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
		<fo:inline font-weight="bold"><xsl:value-of select="eaxs:Name"/>] </fo:inline>
	</xsl:template>

	<xsl:template name="mailbox">
		<xsl:if test="@name">
			<fo:inline font-style="italic">
				<xsl:value-of select="@name"/>
			</fo:inline>
			<xsl:text> </xsl:text>
		</xsl:if>
		<xsl:value-of select="@address"/>
	</xsl:template>

	<xsl:template match="eaxs:SingleBody" mode="RenderToc">
		<fo:list-block  margin="0.25em" padding="0.25em"  border-left="1px solid black" provisional-distance-between-starts="1em" provisional-label-separation="0.25em">
			<xsl:apply-templates select="eaxs:ContentType"/>
			<xsl:apply-templates select="eaxs:Disposition"/>
			<xsl:apply-templates select="eaxs:ContentLanguage"/>
			<xsl:if test="eaxs:ChildMessage">
				<fo:list-item margin-bottom="0.25em">
					<fo:list-item-label end-indent="label-end()"><fo:block></fo:block></fo:list-item-label>
					<fo:list-item-body start-indent="body-start()">
						<xsl:apply-templates select="eaxs:ChildMessage" mode="RenderToc"/>
					</fo:list-item-body>
				</fo:list-item>
			</xsl:if>
		</fo:list-block>
	</xsl:template>
	
	<xsl:template match="eaxs:ChildMessage" mode="RenderToc">
		<fo:list-block margin="0.25em" padding="0.25em" border-left="1px solid black" provisional-distance-between-starts="1em" provisional-label-separation="0.25em">
			<fo:list-item margin-bottom="0.25em">
				<fo:list-item-label end-indent="label-end()"><fo:block></fo:block></fo:list-item-label>
				<fo:list-item-body start-indent="body-start()">
					<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody" mode="RenderToc"/>
				</fo:list-item-body>
			</fo:list-item>
		</fo:list-block>
	</xsl:template>

	<xsl:template match="eaxs:MultiBody" mode="RenderToc">
		<fo:list-block margin="0.25em" padding="0.25em" border-left="1px solid black" provisional-distance-between-starts="1em" provisional-label-separation="0.25em">
			<xsl:apply-templates select="eaxs:ContentType"/>
			<xsl:apply-templates select="eaxs:Disposition"/>
			<xsl:apply-templates select="eaxs:ContentLanguage"/>
			<fo:list-item margin-bottom="0.25em">
				<fo:list-item-label end-indent="label-end()"><fo:block></fo:block></fo:list-item-label>
				<fo:list-item-body start-indent="body-start()">
					<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody" mode="RenderToc"/>
				</fo:list-item-body>
			</fo:list-item>
		</fo:list-block>
	</xsl:template>
	

	<xsl:template match="eaxs:ContentType">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block></fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates/>
					<xsl:call-template name="AttachmentLink"/>
					<xsl:if test="not(fn:lower-case(normalize-space(../@IsAttachment)) = 'true') and (fn:lower-case(normalize-space(.)) = 'text/plain' or fn:lower-case(normalize-space(.)) = 'text/html')">
						<fo:basic-link>
							<xsl:attribute name="internal-destination"><xsl:value-of select="fn:generate-id(../eaxs:BodyContent)"/></xsl:attribute>									
							<fo:inline>&nbsp;</fo:inline><fo:inline xsl:use-attribute-sets="a-link" font-size="small">Go To Content</fo:inline>
						</fo:basic-link>												
					</xsl:if>
					<xsl:if test="following-sibling::eaxs:ContentName != following-sibling::eaxs:DispositionFileName">
						<xsl:text>; name="</xsl:text>
						<xsl:call-template name="escape-specials">
							<xsl:with-param name="text"><xsl:value-of select="following-sibling::eaxs:ContentName"/></xsl:with-param>
						</xsl:call-template>
						<xsl:text>"</xsl:text>
					</xsl:if>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:Disposition">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block></fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates/>
					<xsl:if test="following-sibling::eaxs:DispositionFileName">
						<xsl:text>; filename="</xsl:text>
						<xsl:call-template name="escape-specials">
							<xsl:with-param name="text"><xsl:value-of select="following-sibling::eaxs:DispositionFileName"/></xsl:with-param>
						</xsl:call-template>
						<xsl:text>"</xsl:text>
					</xsl:if>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:ContentLanguage">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block ></fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block>
					<xsl:apply-templates/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>
	
	<xsl:template match="eaxs:MultiBody" mode="RenderContent">
		<xsl:choose>
			<xsl:when test="fn:lower-case(eaxs:ContentType) = 'multipart/alternative'">
				<xsl:for-each select="eaxs:SingleBody | eaxs:MultiBody">
					<xsl:sort select="position()" data-type="number" order="descending"/> <!-- alternatives have priority in descending order, so the last is displayed first -->
					<xsl:apply-templates select="." mode="RenderContent"/>
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody" mode="RenderContent"/>				
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	<xsl:template match="eaxs:SingleBody" mode="RenderContent">
		<!-- only render child messages if they contain plain text or html content -->
		<xsl:apply-templates select="eaxs:BodyContent | eaxs:ExtBodyContent | eaxs:ChildMessage[.//eaxs:SingleBody[fn:lower-case(normalize-space(eaxs:ContentType))='text/plain' or fn:lower-case(normalize-space(eaxs:ContentType)) = 'text/html']] | eaxs:DeliveryStatus"/>
	</xsl:template>
	
	<xsl:template match="eaxs:BodyContent">
		<xsl:if test="not(fn:lower-case(normalize-space(../@IsAttachment)) = 'true') and starts-with(fn:lower-case(normalize-space(../eaxs:ContentType)),'text/')">
			<!-- only render content which is text and which is not an attachment -->
			<fo:block>
				<xsl:attribute name="id"><xsl:value-of select="fn:generate-id(.)"/></xsl:attribute>
				<fox:destination><xsl:attribute name="internal-destination"><xsl:value-of select="fn:generate-id(.)"/></xsl:attribute></fox:destination>
				<xsl:choose>
					<xsl:when test="count(ancestor::eaxs:Message//eaxs:SingleBody[not(fn:lower-case(normalize-space(@IsAttachment)) = 'true') and starts-with(fn:lower-case(normalize-space(eaxs:ContentType)),'text/')]) > 1 ">
						<!-- Only put the header if there are multiple bodies -->
						<fo:block xsl:use-attribute-sets="h3" border-bottom="1.5pt solid black" keep-with-next.within-page="always">Content Type: <xsl:value-of select="../eaxs:ContentType"/></fo:block>				
					</xsl:when>
					<xsl:otherwise>
						<!-- don't put a header, but add an extra line above the content -->
						<fo:block margin-top="1em"> </fo:block>
					</xsl:otherwise>
				</xsl:choose>
				<xsl:apply-templates select="eaxs:Content | eaxs:ContentAsXhtml"/>
			</fo:block>
		</xsl:if>
	</xsl:template>
	
	<xsl:template match="eaxs:ExtBodyContent">
	</xsl:template>
	
	<xsl:template match="eaxs:ChildMessage">
		<fo:block xsl:use-attribute-sets="child-message">
			<xsl:if test="count(ancestor::eaxs:ChildMessage) > 0">
				<xsl:call-template name="hr"/>
			</xsl:if>
			<fo:block xsl:use-attribute-sets="h3">
				<xsl:call-template name="RepeatString"><xsl:with-param name="Count" select="1 + count(ancestor::eaxs:ChildMessage)"></xsl:with-param></xsl:call-template>
				<xsl:text> Child Message </xsl:text> 
				<xsl:value-of select="eaxs:LocalId"/>
			</fo:block>
			<xsl:call-template name="MessageHeaderTocAndContent">
				<xsl:with-param name="RenderToc">false</xsl:with-param>
			</xsl:call-template>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="eaxs:DeliveryStatus">
		<fo:block xsl:use-attribute-sets="delivery-status">
			<xsl:if test="../eaxs:ContentLanguage">
				<xsl:attribute name="xml:lang"><xsl:value-of select="../eaxs:ContentLanguage"/></xsl:attribute>
			</xsl:if>
			<fo:block xsl:use-attribute-sets="h3">Delivery Status</fo:block>
			<fo:block xsl:use-attribute-sets="h4">Per Message Fields</fo:block>
			<fo:list-block provisional-distance-between-starts="10em" provisional-label-separation="0.25em">
				<xsl:for-each select="eaxs:MessageFields/eaxs:Field">
					<fo:list-item>
						<fo:list-item-label end-indent="label-end()"><fo:block><xsl:value-of select="eaxs:Name"/></fo:block></fo:list-item-label>
						<fo:list-item-body  start-indent="body-start()"><fo:block><xsl:value-of select="eaxs:Value"/></fo:block></fo:list-item-body>
					</fo:list-item>					
				</xsl:for-each>
			</fo:list-block>
			<fo:block xsl:use-attribute-sets="h4">Per Recipient Fields</fo:block>
			<fo:list-block provisional-distance-between-starts="10em" provisional-label-separation="0.25em">
				<xsl:for-each select="eaxs:RecipientFields/eaxs:Field">
					<fo:list-item>
						<fo:list-item-label end-indent="label-end()"><fo:block><xsl:value-of select="eaxs:Name"/></fo:block></fo:list-item-label>
						<fo:list-item-body  start-indent="body-start()"><fo:block><xsl:value-of select="eaxs:Value"/></fo:block></fo:list-item-body>
					</fo:list-item>					
				</xsl:for-each>
			</fo:list-block>
			<xsl:if test="../following-sibling::eaxs:SingleBody/eaxs:ChildMessage">
				<xsl:call-template name="hr"/>
			</xsl:if>
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:Content">
		<xsl:choose>
			<xsl:when test="not(normalize-space(.) = '')">
				<!-- treat the same as the html <pre> tag -->
				<fo:block xsl:use-attribute-sets="pre">
					<xsl:if test="../../eaxs:ContentLanguage">
						<xsl:attribute name="xml:lang"><xsl:value-of select="../../eaxs:ContentLanguage"/></xsl:attribute>
					</xsl:if>
					<xsl:call-template name="process-pre"/>
				</fo:block>			
			</xsl:when>	
			<xsl:otherwise>
				<fo:block>
					<xsl:attribute name="id"><xsl:value-of select="fn:generate-id(.)"/></xsl:attribute>
					<fox:destination><xsl:attribute name="internal-destination"><xsl:value-of select="fn:generate-id(.)"/></xsl:attribute></fox:destination>
					<fo:inline font-style="italic">BLANK</fo:inline>					
				</fo:block>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	<xsl:template match="eaxs:ContentAsXhtml">
		<xsl:choose>
			<xsl:when test="not(normalize-space(.) = '')">
				<fo:block>
					<xsl:choose>
						<xsl:when test="html:html/@xml:lang">
							<xsl:attribute name="xml:lang"><xsl:value-of select="html:html/@xml:lang"/></xsl:attribute>
						</xsl:when>
						<xsl:when test="html:html/@lang">
							<xsl:attribute name="xml:lang"><xsl:value-of select="html:html/@lang"/></xsl:attribute>
						</xsl:when>
						<xsl:when test="html:html/html:head/html:meta[fn:lower-case(@http-equiv)='content-language']">
							<xsl:attribute name="xml:lang"><xsl:value-of select="html:html/html:head/html:meta[fn:lower-case(@http-equiv)='content-language']/@content"/></xsl:attribute>							
						</xsl:when>
						<xsl:when test="../../eaxs:ContentLanguage">
							<xsl:attribute name="xml:lang"><xsl:value-of select="../../eaxs:ContentLanguage"/></xsl:attribute>
						</xsl:when>
					</xsl:choose>
					<!-- TODO: Also try to determine xml:lang attribute from html root or html head -->
					<xsl:apply-templates select="html:html/html:body/html:*"/>					
				</fo:block>
			</xsl:when>	
			<xsl:otherwise>
				<fo:block>
					<xsl:attribute name="id"><xsl:value-of select="fn:generate-id(.)"/></xsl:attribute>
					<fox:destination><xsl:attribute name="internal-destination"><xsl:value-of select="fn:generate-id(.)"/></xsl:attribute></fox:destination>
					<fo:inline font-style="italic">BLANK</fo:inline>
				</fo:block>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	<!-- ========================================================================
		Attribute Sets 
	============================================================================= -->
	<xsl:attribute-set name="child-message">
		<xsl:attribute name="margin-top">1em</xsl:attribute>
		<xsl:attribute name="background-color">beige</xsl:attribute>
	</xsl:attribute-set>
	
	<xsl:attribute-set name="delivery-status">
		<xsl:attribute name="margin-top">1em</xsl:attribute>
		<xsl:attribute name="background-color">beige</xsl:attribute>
	</xsl:attribute-set>

	<xsl:attribute-set name="todo">
		<xsl:attribute name="color">red</xsl:attribute>
		<xsl:attribute name="font-size">large</xsl:attribute>
		<xsl:attribute name="font-weight">bold</xsl:attribute>
		<xsl:attribute name="margin-top">1em</xsl:attribute>
	</xsl:attribute-set>

	<!-- ========================================================================
		Some utility templates 
	============================================================================= -->
	
	<!-- join two strings with a path separator, taking into account that the strings might already have a separator or not -->
	<xsl:template name="concat-path">
		<xsl:param name="path1"/>
		<xsl:param name="path2"/>
		<xsl:choose>
			<xsl:when test="not(fn:ends-with($path1,'/')) and not(fn:starts-with($path2,'/'))">
				<xsl:value-of select="fn:concat($path1,'/',$path2)"/>
			</xsl:when>
			<xsl:when test="fn:ends-with($path1,'/') and not(fn:starts-with($path2,'/'))">
				<xsl:call-template name="concat-path">
					<xsl:with-param name="path1" select="fn:substring($path1,1, fn:string-length($path1)-1)"/>
					<xsl:with-param name="path2" select="$path2"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="not(fn:ends-with($path1,'/')) and fn:starts-with($path2,'/')">
				<xsl:call-template name="concat-path">
					<xsl:with-param name="path1" select="$path1"/>
					<xsl:with-param name="path2" select="fn:substring($path2,2)"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="fn:ends-with($path1,'/') and fn:starts-with($path2,'/')">
				<xsl:call-template name="concat-path">
					<xsl:with-param name="path1" select="fn:substring($path1,1, fn:string-length($path1)-1)"/>
					<xsl:with-param name="path2" select="fn:substring($path2,2)"/>
				</xsl:call-template>
			</xsl:when>
		</xsl:choose>
	</xsl:template>
	
	<xsl:template name="RepeatString">
		<xsl:param name="Count" >1</xsl:param>
		<xsl:param name="String">&gt;</xsl:param>
		<xsl:for-each select="1 to $Count">
			<xsl:value-of select="$String"/>
		</xsl:for-each>
	</xsl:template>
	
	<xsl:template name="hr">
		<fo:block><fo:leader leader-pattern="rule" leader-length="100%" rule-style="solid" rule-thickness="1.5pt"/></fo:block>						
	</xsl:template>
	
	<xsl:template name="data-uri">
		<xsl:param name="content-type">text/plain</xsl:param>
		<xsl:param name="transfer-encoding">base64</xsl:param>
		<xsl:param name="content"></xsl:param>
		
		<xsl:variable name="normal-content-type" select="fn:normalize-space(fn:lower-case($content-type))"/>
		<xsl:variable name="normal-transfer-encoding" select="fn:normalize-space(fn:lower-case($transfer-encoding))"/>
		
		<xsl:if test="$normal-transfer-encoding != 'base64' and $normal-transfer-encoding != ''">
			<xsl:message terminate="yes">Unsupported transfer encoding '<xsl:value-of select="$normal-transfer-encoding"/>'</xsl:message>
		</xsl:if>

		<xsl:text>url('data:</xsl:text>
		<xsl:value-of select="$normal-content-type"/>
		<xsl:if test="$normal-transfer-encoding">;<xsl:value-of select="$normal-transfer-encoding"/></xsl:if>
		<xsl:text>,</xsl:text>
		<xsl:choose>
			<xsl:when test="$normal-transfer-encoding = 'base64'">
				<xsl:value-of select="$content"/>			
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="fn:encode-for-uri($content)"/>			
			</xsl:otherwise>
		</xsl:choose>
		<xsl:text>')</xsl:text>
	</xsl:template>
	
	<xsl:template name="replace-string">
		<xsl:param name="text"/>
		<xsl:param name="replace"/>
		<xsl:param name="with"/>
		<xsl:choose>
			<xsl:when test="contains($text,$replace)">
				<xsl:value-of select="substring-before($text,$replace)"/>
				<xsl:value-of select="$with"/>
				<xsl:call-template name="replace-string">
					<xsl:with-param name="text"
						select="substring-after($text,$replace)"/>
					<xsl:with-param name="replace" select="$replace"/>
					<xsl:with-param name="with" select="$with"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$text"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	<!-- for quoted strings replace '\' with '\\' and '"' with '\"' -->
	<xsl:template name="escape-specials">
		<xsl:param name="text"/>
		<xsl:call-template name="replace-string">
			<xsl:with-param name="text">
				<xsl:call-template name="replace-string">
					<xsl:with-param name="text" select="$text"/>
					<xsl:with-param name="replace">\</xsl:with-param>
					<xsl:with-param name="with">\\</xsl:with-param>
				</xsl:call-template>				
			</xsl:with-param>
			<xsl:with-param name="replace">"</xsl:with-param>
			<xsl:with-param name="with">\"</xsl:with-param>
		</xsl:call-template>
	</xsl:template>
	
	<xsl:template name="GetFileExtension">
		<xsl:param name="single-body" select="ancestor-or-self::eaxs:SingleBody[1]"/>
		
		<xsl:choose>
			<!-- force the file extension for certain mime content types, regardless of the content name or disposition filename -->
			<xsl:when test="fn:lower-case(normalize-space($single-body/eaxs:ContentType)) = 'application/pdf'">pdf</xsl:when>
			<xsl:when test="fn:lower-case(normalize-space($single-body/eaxs:ContentType)) = 'text/rtf'">rtf</xsl:when>
			
			<!-- use the extension from the content name or disposition filename if there is one -->
			<xsl:when test="fn:contains($single-body/eaxs:DispositionFilename,'.')"><xsl:value-of select="fn:tokenize($single-body/eaxs:DispositionFilename,'\.')[last()]"/></xsl:when>
			<xsl:when test="fn:contains($single-body/eaxs:ContentName,'.')"><xsl:value-of select="fn:tokenize($single-body/eaxs:ContentName,'\.')[last()]"/></xsl:when>
			
			<!-- fallback to just 'bin' -->
			<xsl:otherwise>bin</xsl:otherwise>
		</xsl:choose>
		
	</xsl:template>
	
	<xsl:template name="AttachmentLink">
		<xsl:param name="single-body" select="ancestor-or-self::eaxs:SingleBody[1]"/>
		
		<xsl:variable name="file-ext"><xsl:call-template name="GetFileExtension"><xsl:with-param name="single-body" select="$single-body"></xsl:with-param></xsl:call-template></xsl:variable>
		
		<xsl:variable name="content-type">
			<xsl:choose>
				<!-- The xep processor tries to process pdf attachments if they have an 'application/pdf' mime type. This will prevent 'slightly corrupt' PDFs from being attached, so instead use the generic octet-stream mime type when embedding using 'data:' urls -->
				<!-- Note this will not help if the pdf is being attached from an external binary file using the 'file:' url -->
				<xsl:when test="$fo-processor='xep' and fn:lower-case(fn:normalize-space($single-body/eaxs:ContentType))='application/pdf'">application/octet-stream</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="$single-body/eaxs:ContentType"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		
		<xsl:if test="$single-body/eaxs:ExtBodyContent or lower-case(normalize-space($single-body/eaxs:BodyContent/eaxs:TransferEncoding)) = 'base64' and (fn:lower-case(normalize-space($single-body/@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space($single-body/eaxs:ContentType)),'text/')))">
			<xsl:choose>
				<xsl:when test="$fo-processor='fop'">
					<xsl:choose>
						<xsl:when test="fn:lower-case(normalize-space($use-embedded-file-link))='true'">
							<fo:basic-link>
								<xsl:attribute name="external-destination">url(embedded-file:<xsl:value-of select="$single-body/eaxs:*/eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="$file-ext"/>)</xsl:attribute>
								<fo:inline> (<fo:inline xsl:use-attribute-sets="a-link" >Open Attachment</fo:inline>)</fo:inline>
							</fo:basic-link>	
						</xsl:when>
						<xsl:otherwise>
							<fo:basic-link>
								<xsl:attribute name="internal-destination">ATT_<xsl:value-of select="$single-body/eaxs:*/eaxs:Hash/eaxs:Value"/></xsl:attribute>
								<fo:inline>&nbsp;</fo:inline><fo:inline xsl:use-attribute-sets="a-link" font-size="small">Go To Attachment</fo:inline>	
							</fo:basic-link>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:when>
				<xsl:when test="$fo-processor='xep'">
					<fo:basic-link>
						<xsl:attribute name="internal-destination">ATT_<xsl:value-of select="$single-body/eaxs:*/eaxs:Hash/eaxs:Value"/></xsl:attribute>
						<fo:inline>&nbsp;</fo:inline><fo:inline xsl:use-attribute-sets="a-link" font-size="small">Go To Attachment</fo:inline>	
					</fo:basic-link>
					<fo:inline font-size="small">&nbsp;&nbsp;
						<rx:pdf-comment>
							<xsl:attribute name="title">Attachment &mdash; </xsl:attribute>
							<rx:pdf-file-attachment icon-type="paperclip">
								<xsl:attribute name="filename"><xsl:value-of select="$single-body/eaxs:*/eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="$file-ext"/></xsl:attribute>
								<xsl:choose>
									<xsl:when test="$single-body/eaxs:ExtBodyContent">
										<xsl:variable name="rel-path">
											<xsl:call-template name="concat-path">
												<xsl:with-param name="path1" select="normalize-space($single-body/ancestor::eaxs:Message/eaxs:RelPath)"/>
												<xsl:with-param name="path2" select="normalize-space($single-body/eaxs:ExtBodyContent/eaxs:RelPath)"/>
											</xsl:call-template>
										</xsl:variable>
										<xsl:choose>
											<xsl:when test="fn:normalize-space(fn:lower-case($single-body/eaxs:ExtBodyContent/eaxs:XMLWrapped)) = 'false'">
												<xsl:attribute name="src">url(<xsl:value-of select="fn:resolve-uri($rel-path, fn:base-uri())"/>)</xsl:attribute>								
											</xsl:when>
											<xsl:when test="fn:normalize-space(fn:lower-case($single-body/eaxs:ExtBodyContent/eaxs:XMLWrapped)) = 'true'">
												<xsl:attribute name="src">
													<xsl:call-template name="data-uri">
														<xsl:with-param name="transfer-encoding" select="document(fn:resolve-uri($rel-path, fn:base-uri()))/eaxs:BodyContent/eaxs:TransferEncoding"/>
														<xsl:with-param name="content-type" select="$content-type"/>
														<xsl:with-param name="content" select="document(fn:resolve-uri($rel-path, fn:base-uri()))/eaxs:BodyContent/eaxs:Content"/>
													</xsl:call-template>
												</xsl:attribute>																
											</xsl:when>
										</xsl:choose>
									</xsl:when>
									<xsl:when test="$single-body/eaxs:BodyContent">
										<xsl:attribute name="src">
											<xsl:call-template name="data-uri">
												<xsl:with-param name="transfer-encoding" select="$single-body/eaxs:BodyContent/eaxs:TransferEncoding"/>
												<xsl:with-param name="content-type" select="$content-type"/>
												<xsl:with-param name="content" select="$single-body/eaxs:BodyContent/eaxs:Content"/>
											</xsl:call-template>
										</xsl:attribute>																
									</xsl:when>
								</xsl:choose>
							</rx:pdf-file-attachment>
						</rx:pdf-comment>
					</fo:inline>								
				</xsl:when>
			</xsl:choose>
		</xsl:if>
	</xsl:template>

	<!-- Functions for filepath manipulation, inspired by https://stackoverflow.com/questions/3116942/doing-file-path-manipulations-in-xslt-->
	<!-- Tried to accomodate both Windows and Linux and URL path separators, probably not as robust as it could be -->
	
	<!-- Given a full path, return just the file name or last path segment component -->
	<xsl:function name="my:GetFileName" as="xs:string">
		<xsl:param name="pfile" as="xs:string"/>
		<xsl:variable name="separator" select="my:GetPathSepartor($pfile)"/>
		<xsl:sequence select="my:ReverseString(substring-before(my:ReverseString($pfile), $separator))" />								
	</xsl:function>
	
	<!-- Given a full path, return just the directory name or complete path minus the last segment, excluding path separator -->
	<xsl:function name="my:GetDirectoryName" as="xs:string">
		<xsl:param name="pfile" as="xs:string"/>
		<xsl:variable name="separator" select="my:GetPathSepartor($pfile)"/>
		<xsl:sequence select="my:ReverseString(substring-after(my:ReverseString($pfile), $separator))" />
	</xsl:function>
	
	<!-- combine two path segments using the appropriate path separator, can accomodate segments having trailing or leading separators without duplicating separators -->
	<xsl:function name="my:Combine" as="xs:string">
		<xsl:param name="p1" as="xs:string"/>
		<xsl:param name="p2" as="xs:string"/>
		<xsl:variable name="separator" select="my:GetPathSepartor(fn:concat($p1,$p2))"/>
		<xsl:variable name="p1x">
			<xsl:choose>
				<xsl:when test="fn:substring($p1, fn:string-length($p1), 1) = $separator"><xsl:value-of select="fn:substring($p1,1,fn:string-length($p1) - 1)"/></xsl:when>
				<xsl:otherwise><xsl:value-of select="$p1"/></xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="p2x">
			<xsl:choose>
				<xsl:when test="fn:substring($p2,1,1) = $separator"><xsl:value-of select="fn:substring($p2,2)"/></xsl:when>
				<xsl:otherwise><xsl:value-of select="$p2"/></xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:value-of select="fn:concat($p1x,$separator, $p2x)"/>
	</xsl:function>
	
	<xsl:function name="my:ReverseString" as="xs:string">
		<xsl:param name="pStr" as="xs:string"/>
		<xsl:sequence select="codepoints-to-string(reverse(string-to-codepoints($pStr)))"/>
	</xsl:function>
	
	<!-- looks at a path string and determines the appropriate separator, defaults to '/', assumes different separators will not be combined in the same path -->
	<xsl:function name="my:GetPathSepartor" as="xs:string">
		<xsl:param name="pfile" as="xs:string"/>
		<xsl:choose>
			<xsl:when test="fn:contains($pfile,'\')">\</xsl:when>
			<xsl:otherwise>/</xsl:otherwise>
		</xsl:choose>		
	</xsl:function>
	
	<!-- Resolves a relative file path against the base file path, similar to the resolve-uri function, except non-western unicode characters are not URL Encoded -->
	<xsl:function name="my:GetPathRelativeToBaseUri" as="xs:string">
		<xsl:param name="pfile" as="xs:string" />
		<xsl:param name="baseUri" as="xs:string"/>
		<xsl:value-of select="my:Combine(my:GetDirectoryName($baseUri), $pfile)"/>
	</xsl:function>
		
</xsl:stylesheet>
