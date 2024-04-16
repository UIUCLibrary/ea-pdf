<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE stylesheet SYSTEM "eaxs_entities.ent">

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

	<xsl:include href="eaxs_xhtml2fo.xsl"/>
	
	<xsl:include href="eaxs_helpers.xsl"/>
	<xsl:include href="eaxs_helpers_test.xsl"/>
	
	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="no" omit-xml-declaration="no" cdata-section-elements="rx:custom-meta"/>
	
	<!-- The font-family values to use for serif, sans-serif, and monospace fonts repsectively -->
	<xsl:param name="SerifFont" select="'serif'"/>
	<xsl:param name="SansSerifFont" select="'sans-serif'"/>
	<xsl:param name="MonospaceFont" select="'monospace'"/>
	<xsl:variable name="DefaultFont" select="$SerifFont"/><!-- Used as the default at the root level of the document, and anywhere else that needs a font which can't be definitely determined -->
	
	<xsl:param name="icc-profile" select="'file:/C:/Program Files/RenderX/XEP/sRGB2014.icc'"/>

	<!-- Which FO Processor -->
	<xsl:param name="fo-processor-version">FOP Version 2.8</xsl:param> <!-- Values used: fop or xep -->
	<xsl:variable name="fo-processor" select="fn:lower-case(fn:tokenize($fo-processor-version)[1])"/>
	<xsl:variable name="producer">UIUCLibrary.EaPdf; <xsl:value-of select="$fo-processor-version"/></xsl:variable>
	
	<xsl:param name="generate-xmp">false</xsl:param><!-- generate the XMP metadata -->
	
	<xsl:param name="list-of-attachments">true</xsl:param>
	
	<!-- page size -->
	<xsl:param name="page-width">8.5in</xsl:param>
	<xsl:param name="page-height">11in</xsl:param>
	<xsl:param name="page-margin-top">1in</xsl:param>
	<xsl:param name="page-margin-bottom">1in</xsl:param>
	<xsl:param name="page-margin-left">1in</xsl:param>
	<xsl:param name="page-margin-right">1in</xsl:param>
	
	<xsl:param name="dpi">96</xsl:param><!-- dots per inch, may need to match values in the XSL FO processor config file -->
	
	<!-- if there are multiple PDF files making up the email archive -->
	<xsl:param name="ContinuedFrom"></xsl:param>
	<xsl:param name="ContinuedIn"></xsl:param>
	
	<xsl:template name="check-params">
		<xsl:choose>
			<xsl:when test="$fo-processor='fop'"/>
			<xsl:when test="$fo-processor='xep'"/>
			<xsl:otherwise>
				<xsl:message terminate="yes">
					<xsl:text>Error: (name='check-params') The value '</xsl:text><xsl:value-of select="$fo-processor-version"/><xsl:text>' is not a valid value for fo-processor-version param; allowed values must start with 'FOP' or 'XEP'.</xsl:text>
				</xsl:message>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="/">
		
		<xsl:call-template name="check-params"/>
		
		<xsl:call-template name="test-helpers"/>
		
		<xsl:if test="$fo-processor='xep'">
			<!-- Add ICC color profile -->
			<xsl:processing-instruction name="xep-pdf-icc-profile">url(<xsl:value-of select="$icc-profile"/>)</xsl:processing-instruction>
			<xsl:processing-instruction name="xep-pdf-view-mode">show-bookmarks</xsl:processing-instruction>
			<xsl:processing-instruction name="xep-pdf-drop-unused-destinations">false</xsl:processing-instruction>
			<!-- XEP doesn't seem to have support for /ViewerPreferences/NonFullScreenPageMode/UseOutlines -->
		</xsl:if>
		
		<fo:root>
			<xsl:attribute name="font-family"><xsl:value-of select="$DefaultFont"/></xsl:attribute>
			<xsl:if test="$fo-processor='xep'">
				<xsl:call-template name="xep-metadata"/>
			</xsl:if>
			<fo:layout-master-set>
				<fo:simple-page-master master-name="message-page" page-width="{$page-width}" page-height="{$page-height}">
					<fo:region-body margin-top="{$page-margin-top}" margin-bottom="{$page-margin-bottom}" margin-left="{$page-margin-left}"
						margin-right="{$page-margin-right}"/>
					<fo:region-before extent="{$page-margin-top}"/>
					<fo:region-after extent="{$page-margin-bottom}"/>
				</fo:simple-page-master>
			</fo:layout-master-set>
			
			<xsl:call-template name="declarations"/>
			
			<xsl:call-template name="bookmarks"/>
			
			<fo:page-sequence master-reference="message-page" xml:lang="en">
				<xsl:call-template name="static-content"/>
				<fo:flow flow-name="xsl-region-body">
					<xsl:call-template name="CoverPage"/>
				</fo:flow>
			</fo:page-sequence>
			
			<fo:page-sequence master-reference="message-page">
				<xsl:call-template name="static-content"/>
				<xsl:for-each select="/eaxs:Account/eaxs:Folder[eaxs:Message or eaxs:Folder]">
					<fo:flow flow-name="xsl-region-body">
						<fo:block><!-- empty block just to use as link destination -->
							<xsl:call-template name="tag-artifact"/>
							<xsl:attribute name="id"><xsl:value-of select="generate-id(.)"/></xsl:attribute>
							<fox:destination><xsl:attribute name="internal-destination"><xsl:value-of select="generate-id(.)"/></xsl:attribute></fox:destination>
						</fo:block>
						<xsl:apply-templates select="." mode="RenderContent"/>
					</fo:flow>
				</xsl:for-each>
			</fo:page-sequence>
			
			<xsl:if test="$list-of-attachments='true'">
				<fo:page-sequence master-reference="message-page" xml:lang="en">
					<xsl:call-template name="static-content"/>
					<fo:flow flow-name="xsl-region-body">
						<xsl:call-template name="AttachmentsList"/>
					</fo:flow>
				</fo:page-sequence>
			</xsl:if>
			
			<!-- TODO: Add a section which is the conversion report with info, warning, and error messages -->
			<!--       Might need to embedd these into the source XML, not just as xml comments -->
		</fo:root>
	</xsl:template>
	
	<xsl:template name="static-content">
		<fo:static-content flow-name="xsl-region-before">
			<xsl:call-template name="tag-artifact"><xsl:with-param name="type" select="'Pagination'"/><xsl:with-param name="subtype" select="'Header'"/></xsl:call-template>
			<fo:block xml:lang="und" text-align="center" margin-left="1in" margin-right="1in">Account:
				<xsl:value-of select="/eaxs:Account/eaxs:EmailAddress[1]"/> Folder:
				<xsl:value-of select="/eaxs:Account/eaxs:Folder/eaxs:Name"/></fo:block>
		</fo:static-content>
		<fo:static-content flow-name="xsl-region-after">
			<xsl:call-template name="tag-artifact"><xsl:with-param name="type" select="'Pagination'"/><xsl:with-param name="subtype" select="'Footer'"/></xsl:call-template>
			<fo:block xml:lang="en" text-align="center" margin-left="1in" margin-right="1in">
				<fo:page-number/>
			</fo:block>
		</fo:static-content>		
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
		<xsl:variable name="font-size" select="'95%'"/>
		
		<fo:block id="AttachmentList" xsl:use-attribute-sets="h1"><xsl:call-template name="tag-H1"/><fox:destination internal-destination="AttachmentList"/>All Attachments</fo:block>
		
		<fo:block background-color="beige" border="1px solid brown" padding="0.125em">
			You may need to open the PDF reader's attachments list to download or open these f&zwnj;iles. 
			The same attachment might have originated from multiple mail messages, possibly with different f&zwnj;ilenames, but only one copy will be attached to this PDF; 
			however, each message will have a seperate annotation pointing to the same attachment.
		</fo:block>
		
		<fo:block xsl:use-attribute-sets="h2"><xsl:call-template name="tag-H2"/>Source Email Files</fo:block>
		
		<xsl:choose>
			<xsl:when test="//eaxs:Folder[eaxs:Message or eaxs:Folder]/eaxs:FolderProperties[eaxs:RelPath] or //eaxs:Folder/eaxs:Message/eaxs:MessageProperties[eaxs:RelPath]">
				<fo:list-block font-size="{$font-size}">
					<!-- Source MBOX files are referenced in FolderProperties -->
					<xsl:for-each select="//eaxs:Folder[eaxs:Message or eaxs:Folder]/eaxs:FolderProperties[eaxs:RelPath]">
						<xsl:sort select="fn:string-join(ancestor-or-self::eaxs:Folder/eaxs:Name)"/> 
						<xsl:variable name="UniqueDestination">
							<xsl:text>X_</xsl:text><xsl:value-of select="eaxs:Hash/eaxs:Value"/>
						</xsl:variable>
						<fo:list-item margin-top="5pt" >
							<fo:list-item-label>
								<fo:block font-weight="bold"><fo:inline>Source File <xsl:value-of select="position()"/>:</fo:inline></fo:block>
							</fo:list-item-label>
							<fo:list-item-body keep-together.within-column="always">
								<xsl:call-template name="file-list-2x2">
									<xsl:with-param name="lbl-1">F&zwnj;ile name: </xsl:with-param>
									<xsl:with-param name="body-1">
										<xsl:call-template name="InsertZwspAfterNonWords"><xsl:with-param name="string" select="eaxs:RelPath"/></xsl:call-template>
										<xsl:if test="eaxs:Size">
											<fo:inline font-size="small"> (<xsl:value-of select="fn:format-number(eaxs:Size,'#,###')"/> bytes)</fo:inline>
										</xsl:if>
									</xsl:with-param>
									<xsl:with-param name="lbl-2">Folder name: </xsl:with-param>
									<xsl:with-param name="body-2">
										<xsl:for-each select="ancestor-or-self::eaxs:Folder/eaxs:Name">
											<xsl:choose>
												<xsl:when test="position() != last()">
													<xsl:value-of select="."/><xsl:text>&rarr;&zwnj;</xsl:text>
												</xsl:when>
												<xsl:otherwise>
													<fo:basic-link>
														<xsl:attribute name="internal-destination">M<xsl:value-of select="$UniqueDestination"/></xsl:attribute>
														<fo:inline xsl:use-attribute-sets="a-link">
															<xsl:value-of select="."/>
														</fo:inline>	
													</fo:basic-link>											
												</xsl:otherwise>
											</xsl:choose>
										</xsl:for-each>
									</xsl:with-param>
								</xsl:call-template>
							</fo:list-item-body>
						</fo:list-item>
					</xsl:for-each>
					
					<!-- Source EML files are referenced in MessageProperties -->
					<xsl:for-each select="//eaxs:Folder/eaxs:Message/eaxs:MessageProperties[eaxs:RelPath]">
						<xsl:sort select="eaxs:RelPath"/>
						<xsl:variable name="UniqueDestination">
							<xsl:text>MX_</xsl:text><xsl:value-of select="eaxs:Hash/eaxs:Value"/>
						</xsl:variable>
						<fo:list-item margin-top="5pt" >
							<fo:list-item-label>
								<fo:block font-weight="bold"><fo:inline>Source File <xsl:value-of select="position()"/>:</fo:inline></fo:block>
							</fo:list-item-label>
							<fo:list-item-body keep-together.within-column="always">
								<xsl:call-template name="file-list-2x2">
									<xsl:with-param name="lbl-1">F&zwnj;ile name: </xsl:with-param>
									<xsl:with-param name="body-1">
										<xsl:call-template name="InsertZwspAfterNonWords"><xsl:with-param name="string" select="eaxs:RelPath"/></xsl:call-template>
										<xsl:if test="eaxs:Size">
											<fo:inline font-size="small"> (<xsl:value-of select="fn:format-number(eaxs:Size,'#,###')"/> bytes)</fo:inline>
										</xsl:if>
									</xsl:with-param>
									<xsl:with-param name="lbl-2">Message id: </xsl:with-param>
									<xsl:with-param name="body-2">
										<fo:basic-link>
											<xsl:attribute name="internal-destination"><xsl:value-of select="$UniqueDestination"/></xsl:attribute>
											<fo:inline xsl:use-attribute-sets="a-link" >
												<xsl:call-template name="InsertZwspAfterNonWords"><xsl:with-param name="string" select="../eaxs:MessageId"/></xsl:call-template>
											</fo:inline>	
										</fo:basic-link>
									</xsl:with-param>
								</xsl:call-template>
							</fo:list-item-body>
						</fo:list-item>
					</xsl:for-each>
				</fo:list-block>
			</xsl:when>
			<xsl:when test="/eaxs:Account/processing-instruction('ContinuedIn')">
				<fo:block>
					Email messages are split into multiple PDF files; the last PDF file in the sequence contains the source files.
					The next file in the sequence is '<fo:basic-link xsl:use-attribute-sets="a-link" external-destination="url('{$ContinuedIn}')" show-destination="new"><xsl:value-of select="$ContinuedIn"/></fo:basic-link>'.
				</fo:block>
			</xsl:when>
			<xsl:otherwise>
				<fo:block>
					The sources file(s) for this PDF email archive are not available as attachments.
				</fo:block>
				<xsl:message terminate="no">WARNING: The sources file(s) for this email archive are not available as PDF attachments.</xsl:message>
			</xsl:otherwise>
		</xsl:choose>
		
		<!-- All Attachments -->
		<xsl:if test="//eaxs:SingleBody/eaxs:ExtBodyContent | //eaxs:SingleBody/eaxs:BodyContent[fn:lower-case(normalize-space(../@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(../eaxs:ContentType)),'text/'))]">
			<fo:block xsl:use-attribute-sets="h2"><xsl:call-template name="tag-H2"/>File Attachments</fo:block>
			
			<fo:list-block font-size="{$font-size}">
				<xsl:for-each-group select="//eaxs:SingleBody/eaxs:ExtBodyContent | //eaxs:SingleBody/eaxs:BodyContent[fn:lower-case(normalize-space(../@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(../eaxs:ContentType)),'text/'))]" group-by="eaxs:Hash/eaxs:Value">
					<xsl:sort select="../eaxs:DispositionFileName"/>
					<xsl:sort select="../eaxs:ContentName"/>
					<xsl:variable name="hash" select="eaxs:Hash/eaxs:Value"/>
					<xsl:variable name="multiple-names" select="count(fn:distinct-values(//eaxs:SingleBody[*/eaxs:Hash/eaxs:Value = $hash]/(eaxs:DispositionFileName | eaxs:ContentName))) > 1"/>
					<xsl:variable name="multiple-msgs" select="count(//eaxs:SingleBody[*/eaxs:Hash/eaxs:Value = $hash]/ancestor::*[eaxs:MessageId][1]/eaxs:MessageId) > 1"/>					
					<fo:list-item margin-top="5pt">
						<fo:list-item-label>
							<fo:block font-weight="bold"><fo:inline>Attachment <xsl:value-of select="position()"/>:</fo:inline></fo:block>						
						</fo:list-item-label>
						<fo:list-item-body keep-together.within-column="always">
							<xsl:call-template name="file-list-2x2">
								<xsl:with-param name="lbl-1">
									<xsl:text>F&zwnj;ile name</xsl:text>
									<xsl:if test="$multiple-names">s</xsl:if>
									<xsl:text>:</xsl:text>
								</xsl:with-param>
								<xsl:with-param name="body-1">
									<xsl:choose>
										<xsl:when test="../eaxs:DispositionFileName | ../eaxs:ContentName">
											<xsl:if test="$multiple-names">
												<xsl:text>"</xsl:text>
											</xsl:if>
											<xsl:value-of select="fn:string-join(fn:distinct-values(//eaxs:SingleBody[*/eaxs:Hash/eaxs:Value = $hash]/(eaxs:DispositionFileName | eaxs:ContentName)),'&quot;, &quot;')"/>
											<xsl:if test="$multiple-names">
												<xsl:text>"</xsl:text>
											</xsl:if>
										</xsl:when>
										<xsl:otherwise>
											<fo:inline xsl:use-attribute-sets="i">* No f&zwnj;ilename given *</fo:inline>
										</xsl:otherwise>
									</xsl:choose>
									<xsl:if test="eaxs:Size">
										<fo:inline font-size="small"> (<xsl:value-of select="fn:format-number(eaxs:Size,'#,###')"/> bytes)</fo:inline>
									</xsl:if>
								</xsl:with-param>
								<xsl:with-param name="lbl-2">
									<xsl:text>Message id</xsl:text>
									<xsl:if test="$multiple-msgs">s</xsl:if>
									<xsl:text>:</xsl:text>
								</xsl:with-param>
								<xsl:with-param name="body-2">
									<xsl:for-each select="//eaxs:SingleBody[*/eaxs:Hash/eaxs:Value = $hash]/ancestor::*[eaxs:MessageId][1]/eaxs:MessageId">
										<fo:block>
											<fo:basic-link xsl:use-attribute-sets="a-link">
												<xsl:attribute name="internal-destination"><xsl:text>MESSAGE_</xsl:text><xsl:value-of select="ancestor::*[eaxs:MessageId][last()]/eaxs:LocalId"/></xsl:attribute>
												<xsl:call-template name="InsertZwspAfterNonWords"><xsl:with-param name="string" select="."/></xsl:call-template>												
											</fo:basic-link>
										</fo:block>
									</xsl:for-each>
								</xsl:with-param>
							</xsl:call-template>								
						</fo:list-item-body>
					</fo:list-item>
				</xsl:for-each-group>
			</fo:list-block>
		</xsl:if>
	</xsl:template>
	
	<!-- template for a 2-item list used for the attachments lists -->
	<xsl:template name="file-list-2x2">
		<xsl:param name="starts">7em</xsl:param>
		<xsl:param name="lbl-1">LABEL-1</xsl:param>
		<xsl:param name="body-1">BODY-1</xsl:param>
		<xsl:param name="lbl-2">LABEL-2</xsl:param>
		<xsl:param name="body-2">BODY-2</xsl:param>
		
		<fo:list-block margin-top="1.2em" margin-left="1em" provisional-distance-between-starts="{$starts}" provisional-label-separation="5pt" >
			<fo:list-item>
				<fo:list-item-label end-indent="label-end()">
					<fo:block font-weight="bold"><xsl:copy-of select="$lbl-1"/></fo:block>
				</fo:list-item-label>
				<fo:list-item-body start-indent="body-start()">
					<fo:block>
						<xsl:copy-of select="$body-1"/>
					</fo:block>
				</fo:list-item-body>
			</fo:list-item>
			<fo:list-item>
				<fo:list-item-label end-indent="label-end()">
					<fo:block font-weight="bold"><xsl:copy-of select="$lbl-2"/></fo:block>
				</fo:list-item-label>
				<fo:list-item-body start-indent="body-start()">
					<fo:block>
						<xsl:copy-of select="$body-2"/>
					</fo:block>
				</fo:list-item-body>
			</fo:list-item>
		</fo:list-block>	
	</xsl:template>
	
	<xsl:template name="bookmarks">
		<fo:bookmark-tree>
			<xsl:apply-templates select="/eaxs:Account/eaxs:Folder[eaxs:Message or eaxs:Folder]" mode="RenderBookmarks"></xsl:apply-templates>
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
			<xsl:if test="count(eaxs:Message) > 0">
				<fo:bookmark starting-state="hide">
					<xsl:attribute name="internal-destination"><xsl:value-of select="generate-id(.)"/></xsl:attribute>
					<fo:bookmark-title><xsl:value-of select="count(eaxs:Message)"/> Messages</fo:bookmark-title>
					<xsl:apply-templates select="eaxs:Message" mode="RenderBookmarks"/>
				</fo:bookmark>
			</xsl:if>
			<xsl:if test="eaxs:Folder[eaxs:Message or eaxs:Folder]">
				<fo:bookmark starting-state="hide">
					<xsl:attribute name="internal-destination"><xsl:value-of select="generate-id(eaxs:Folder[eaxs:Message or eaxs:Folder][1])"/></xsl:attribute>
					<fo:bookmark-title><xsl:value-of select="count(eaxs:Folder[eaxs:Message or eaxs:Folder])"/> Sub-folders</fo:bookmark-title>
					<xsl:apply-templates select="eaxs:Folder[eaxs:Message or eaxs:Folder]" mode="RenderBookmarks"><xsl:with-param name="topfolder">false</xsl:with-param></xsl:apply-templates>
				</fo:bookmark>
			</xsl:if>
		</fo:bookmark>
		<xsl:if test="$topfolder='true' and $list-of-attachments='true'">
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
				<pdf:catalog>
					<pdf:name key="PageMode">UseOutlines</pdf:name>
					<!-- TODO:  Maybe set /ViewerPreferences/NonFullScreenPageMode/UseOutlines -->
				</pdf:catalog>
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
		<!-- NOTE:  This is just a placeholder for possible future use; the XMP is generated during a post-processing step in the EAXS to PDF processing step -->
	</xsl:template>
	
	<xsl:template name="declarations-attachments">
		<!-- if the source is an MBOX file, it is referenced in Folder/FolderProperties -->
		<xsl:for-each select="//eaxs:Folder[eaxs:Message or eaxs:Folder]/eaxs:FolderProperties[eaxs:RelPath]">
			<pdf:embedded-file>
				<xsl:attribute name="filename"><xsl:value-of select="eaxs:Hash/eaxs:Value"/><xsl:value-of select="eaxs:FileExt"/></xsl:attribute>
				<xsl:attribute name="description">Source f&zwnj;ile for mail folder '<xsl:value-of select="../eaxs:Name"/>'</xsl:attribute>
				<xsl:attribute name="src">url(<xsl:value-of select="fn:resolve-uri(eaxs:RelPath, fn:base-uri())"/>)</xsl:attribute>
			</pdf:embedded-file>				
		</xsl:for-each>
		<!-- if the source is an EML file, it is referenced in Folder/Message/MessageProperties -->
		<xsl:for-each select="//eaxs:Folder/eaxs:Message/eaxs:MessageProperties[eaxs:RelPath]">
			<pdf:embedded-file>
				<xsl:attribute name="filename"><xsl:value-of select="eaxs:Hash/eaxs:Value"/><xsl:value-of select="eaxs:FileExt"/></xsl:attribute>
				<xsl:attribute name="description">Source f&zwnj;ile for message '<xsl:value-of select="../eaxs:MessageId"/>'</xsl:attribute>
				<xsl:attribute name="src">url(<xsl:value-of select="fn:resolve-uri(eaxs:RelPath, fn:base-uri())"/>)</xsl:attribute>
			</pdf:embedded-file>				
		</xsl:for-each>
		<!-- inline attachments which are not text -->
		<xsl:for-each-group select="//eaxs:SingleBody/eaxs:BodyContent[fn:lower-case(normalize-space(../@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(../eaxs:ContentType)),'text/'))]" group-by="eaxs:Hash/eaxs:Value">
			<xsl:variable name="filename">
				<xsl:value-of select="eaxs:Hash/eaxs:Value"/><xsl:call-template name="GetFileExtension"/>
			</xsl:variable>
			<pdf:embedded-file>
				<xsl:attribute name="filename"><xsl:value-of select="$filename"/></xsl:attribute>
				<xsl:attribute name="description">Original File Name: <xsl:value-of select="(../eaxs:DispositionFileName | ../eaxs:ContentName)[1]"/></xsl:attribute>
				<xsl:attribute name="src">
					<xsl:call-template name="data-uri">
						<xsl:with-param name="content-type" select="../eaxs:ContentType"/>
						<xsl:with-param name="transfer-encoding" select="eaxs:TransferEncoding"/>
						<xsl:with-param name="content" select="eaxs:Content"/>
					</xsl:call-template>
				</xsl:attribute>
			</pdf:embedded-file>								
		</xsl:for-each-group>
		<!-- external attachments -->
		<xsl:for-each-group select="//eaxs:SingleBody/eaxs:ExtBodyContent" group-by="eaxs:Hash/eaxs:Value">
			<xsl:variable name="rel-path">
				<xsl:call-template name="concat-path">
					<xsl:with-param name="path1" select="normalize-space(ancestor::eaxs:Message/eaxs:RelPath)"/>
					<xsl:with-param name="path2" select="normalize-space(eaxs:RelPath)"/>
				</xsl:call-template>
			</xsl:variable>
			<xsl:variable name="filename">
				<xsl:value-of select="eaxs:Hash/eaxs:Value"/><xsl:call-template name="GetFileExtension"/>
			</xsl:variable>
			<pdf:embedded-file>
				<xsl:attribute name="filename"><xsl:value-of select="$filename"/></xsl:attribute>
				<xsl:attribute name="description">Original File Name: <xsl:value-of select="(../eaxs:DispositionFileName | ../eaxs:ContentName)[1]"/></xsl:attribute>
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
		</xsl:for-each-group>		
	</xsl:template>
	
	<xsl:template name="CoverPage">
		<fo:block xsl:use-attribute-sets="h1" id="CoverPage"><xsl:call-template name="tag-H1"/><fox:destination internal-destination="CoverPage"/>PDF Email Archive (PDF/mail-1m)</fo:block>
		
		<fo:list-block>
			<fo:list-item xsl:use-attribute-sets="h2-font h2-space">
				<fo:list-item-label><fo:block>Created On: </fo:block></fo:list-item-label>
				<fo:list-item-body start-indent="6em"><fo:block><xsl:value-of select="fn:format-dateTime(fn:current-dateTime(), '[FNn], [MNn] [D], [Y], [h]:[m]:[s] [PN]')"/></fo:block></fo:list-item-body>
			</fo:list-item>
			
			<fo:list-item xsl:use-attribute-sets="h2-font h2-space">
				<fo:list-item-label><fo:block>Created By: </fo:block></fo:list-item-label>
				<fo:list-item-body start-indent="6em"><fo:block><xsl:value-of select="$producer"/></fo:block></fo:list-item-body>
			</fo:list-item>
			
			<xsl:if test="/eaxs:Account/eaxs:EmailAddress">
				<fo:list-item>
					<fo:list-item-label xsl:use-attribute-sets="h2-font h2-space"><fo:block>Accounts (<xsl:value-of select="count(/eaxs:Account/eaxs:EmailAddress)"/>): </fo:block></fo:list-item-label>
					<fo:list-item-body>
						<fo:list-block margin-top="2em" margin-left="1em">
							<xsl:for-each select="/eaxs:Account/eaxs:EmailAddress">
								<fo:list-item>
									<fo:list-item-label xsl:use-attribute-sets="h3-font" end-indent="label-end()"><fo:block><xsl:call-template name="tag-Span"/>&#x2022;</fo:block></fo:list-item-label>
									<fo:list-item-body xsl:use-attribute-sets="h3-font" start-indent="body-start()"><fo:block><xsl:call-template name="tag-Span"/><xsl:apply-templates/></fo:block></fo:list-item-body>
								</fo:list-item>
							</xsl:for-each>
						</fo:list-block>
					</fo:list-item-body>
				</fo:list-item>
			</xsl:if>
			
			<fo:list-item xsl:use-attribute-sets="h2-font h2-space">
				<fo:list-item-label><fo:block>Global Id: </fo:block></fo:list-item-label>
				<fo:list-item-body start-indent="5em"><fo:block><xsl:value-of select="/eaxs:Account/eaxs:GlobalId"/></fo:block></fo:list-item-body>
			</fo:list-item>
			
			<xsl:if test="$ContinuedIn or $ContinuedFrom">
				<fo:list-item>
					<fo:list-item-label xsl:use-attribute-sets="h2-font h2-space"><fo:block>Multipart Archive: </fo:block></fo:list-item-label>
					<fo:list-item-body>
						<fo:list-block margin-top="2em" margin-left="1em">
							<xsl:if test="$ContinuedFrom">
								<fo:list-item>
									<fo:list-item-label xsl:use-attribute-sets="h3-font" end-indent="label-end()"><fo:block><xsl:call-template name="tag-Span"/>&#x2022;</fo:block></fo:list-item-label>
									<fo:list-item-body xsl:use-attribute-sets="h3-font" start-indent="body-start()"><fo:block><xsl:call-template name="tag-Span"/><fo:basic-link xsl:use-attribute-sets="a-link" external-destination="url('{$ContinuedFrom}')" show-destination="new">Previous: <xsl:value-of select="$ContinuedFrom"/></fo:basic-link></fo:block></fo:list-item-body>
								</fo:list-item>
							</xsl:if>
							<xsl:if test="$ContinuedIn">
								<fo:list-item>
									<fo:list-item-label xsl:use-attribute-sets="h3-font" end-indent="label-end()"><fo:block><xsl:call-template name="tag-Span"/>&#x2022;</fo:block></fo:list-item-label>
									<fo:list-item-body xsl:use-attribute-sets="h3-font" start-indent="body-start()"><fo:block><xsl:call-template name="tag-Span"/><fo:basic-link xsl:use-attribute-sets="a-link" external-destination="url('{$ContinuedIn}')" show-destination="new">Next: <xsl:value-of select="$ContinuedIn"/></fo:basic-link></fo:block></fo:list-item-body>
								</fo:list-item>
							</xsl:if>
						</fo:list-block>
					</fo:list-item-body>
				</fo:list-item>
			</xsl:if>
			
			<!-- QUESTION: Do not count child messages? -->
			<fo:list-item xsl:use-attribute-sets="h2-font h2-space">
				<fo:list-item-label><fo:block>Message Count: </fo:block></fo:list-item-label>
				<fo:list-item-body start-indent="8em"><fo:block><xsl:value-of select="count(//eaxs:Message)"/></fo:block></fo:list-item-body>
			</fo:list-item>
			
			<!-- QUESTION: Only count distinct attachments, based on the hash? -->
			<fo:list-item xsl:use-attribute-sets="h2-font h2-space">
				<fo:list-item-label><fo:block>Attachment Count: </fo:block></fo:list-item-label>
				<fo:list-item-body start-indent="9.5em"><fo:block><xsl:value-of select="count(//eaxs:SingleBody[(eaxs:ExtBodyContent or fn:lower-case(normalize-space(eaxs:BodyContent/eaxs:TransferEncoding)) = 'base64') and (fn:lower-case(normalize-space(@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(eaxs:ContentType)),'text/')))])"/></fo:block></fo:list-item-body>
			</fo:list-item>
			
			<fo:list-item>
				<fo:list-item-label  xsl:use-attribute-sets="h2-font h2-space"><fo:block>Folders (<xsl:value-of select="count(/eaxs:Account//eaxs:Folder[eaxs:Message or eaxs:Folder])"/>): </fo:block></fo:list-item-label>
				<fo:list-item-body>
					<fo:block margin-top="2em">
						<xsl:apply-templates select="/eaxs:Account/eaxs:Folder[eaxs:Message or eaxs:Folder]" mode="RenderToc"/>
					</fo:block>
				</fo:list-item-body>
			</fo:list-item>
			
		</fo:list-block>
	</xsl:template>
	
	<xsl:template match="eaxs:Folder" mode="RenderToc">
		<fo:list-block margin-left="1em">
			<fo:list-item>
				<fo:list-item-label end-indent="label-end()" xsl:use-attribute-sets="h3-font"><fo:block>&#x2022;</fo:block></fo:list-item-label>
				<fo:list-item-body start-indent="body-start()" >
					<fo:block xsl:use-attribute-sets="h3-font">
						<xsl:apply-templates select="eaxs:Name"/>
						<xsl:if test="(count(eaxs:Message) > 0) or (eaxs:FolderProperties[eaxs:RelPath] | eaxs:Message/eaxs:MessageProperties[eaxs:RelPath])">
							<fo:inline font-size="small"> (</fo:inline>
							<xsl:if test="count(eaxs:Message) > 0">
								<fo:inline font-size="small"><xsl:value-of select="count(eaxs:Message)"/> Messages</fo:inline>
							</xsl:if>
							<xsl:if test="(count(eaxs:Message) > 0) and (eaxs:FolderProperties[eaxs:RelPath] | eaxs:Message/eaxs:MessageProperties[eaxs:RelPath])">
								<fo:inline font-size="small">, </fo:inline>
							</xsl:if>
						</xsl:if>
						<xsl:for-each select="eaxs:FolderProperties[eaxs:RelPath] | eaxs:Message/eaxs:MessageProperties[eaxs:RelPath]">
							<xsl:variable name="UniqueDestination">
								<xsl:text>X_</xsl:text><xsl:value-of select="eaxs:Hash/eaxs:Value"/>
							</xsl:variable>
							<xsl:choose>
								<xsl:when test="$fo-processor='fop'">
									<fo:inline font-size="small">
										<xsl:attribute name="id">M<xsl:value-of select="$UniqueDestination"/></xsl:attribute>
										<fox:destination><xsl:attribute name="internal-destination">M<xsl:value-of select="$UniqueDestination"/></xsl:attribute></fox:destination>
										<xsl:if test="fn:position()=1">									
											<fo:inline>Source</fo:inline>
										</xsl:if>
										<fo:inline>&nbsp;</fo:inline>
										<fo:basic-link>
											<xsl:attribute name="id"><xsl:value-of select="$UniqueDestination"/></xsl:attribute>
											<xsl:attribute name="internal-destination"><xsl:value-of select="$UniqueDestination"/></xsl:attribute>
											<fox:destination><xsl:attribute name="internal-destination"><xsl:value-of select="$UniqueDestination"/></xsl:attribute></fox:destination>
											<fo:inline xsl:use-attribute-sets="a-link" font-size="small">&nbsp;&nbsp;</fo:inline>	
										</fo:basic-link>
									</fo:inline>
								</xsl:when>
								<xsl:when test="$fo-processor='xep'">
									<fo:inline font-size="small">
										<xsl:attribute name="id">M<xsl:value-of select="$UniqueDestination"/></xsl:attribute>
										<fox:destination><xsl:attribute name="internal-destination">M<xsl:value-of select="$UniqueDestination"/></xsl:attribute></fox:destination>
										<xsl:if test="fn:position()=1">
											<fo:inline>Source</fo:inline>
										</xsl:if>
										<fo:inline>&nbsp;</fo:inline>
										<rx:pdf-comment>
											<xsl:attribute name="title">Source File &mdash; <xsl:value-of select="eaxs:RelPath"/></xsl:attribute>
											<rx:pdf-file-attachment icon-type="paperclip">
												<xsl:attribute name="filename"><xsl:value-of select="eaxs:Hash/eaxs:Value"/><xsl:value-of select="eaxs:FileExt"/></xsl:attribute>
												<xsl:attribute name="src">url(<xsl:value-of select="my:GetPathRelativeToBaseUri(eaxs:RelPath, fn:base-uri())"/>)</xsl:attribute>
											</rx:pdf-file-attachment>
										</rx:pdf-comment>
										<fo:inline>&nbsp;&nbsp;</fo:inline>
									</fo:inline>
								</xsl:when>
								<xsl:when test="$fo-processor!='xep' and $fo-processor!='fop'">
									<xsl:message terminate="yes">
										<xsl:text>Error: (match='eaxs:Folder' mode='RenderToc') The value '</xsl:text><xsl:value-of select="$fo-processor-version"/><xsl:text>' is not a valid value for fo-processor-version param; allowed values must start with 'FOP' or 'XEP'.</xsl:text>
									</xsl:message>
								</xsl:when>
							</xsl:choose>
						</xsl:for-each>
						<xsl:if test="(count(eaxs:Message) > 0) or (eaxs:FolderProperties[eaxs:RelPath] | eaxs:Message/eaxs:MessageProperties[eaxs:RelPath])">
							<fo:inline font-size="small">) </fo:inline>
						</xsl:if>
						<xsl:if test="count(eaxs:Message) > 0">
							<fo:basic-link>
								<xsl:attribute name="internal-destination"><xsl:value-of select="generate-id(.)"/></xsl:attribute>
								<fo:inline>&nbsp;</fo:inline><fo:inline xsl:use-attribute-sets="a-link" font-size="small">Go To First Message</fo:inline>
							</fo:basic-link>
						</xsl:if>
					</fo:block>
					<xsl:apply-templates select="eaxs:Folder[eaxs:Message or eaxs:Folder]" mode="RenderToc"/>
				</fo:list-item-body>
			</fo:list-item>
		</fo:list-block>
	</xsl:template>
	
	<xsl:template match="eaxs:Folder" mode="RenderContent">
		<xsl:apply-templates select="eaxs:Message" />
		<xsl:for-each select="eaxs:Folder[eaxs:Message or eaxs:Folder]">
			<fo:block><xsl:call-template name="tag-Sect"/>
				<xsl:attribute name="id"><xsl:value-of select="generate-id(.)"/></xsl:attribute>
				<fox:destination><xsl:attribute name="internal-destination"><xsl:value-of select="generate-id(.)"/></xsl:attribute></fox:destination>
				<xsl:apply-templates select="." mode="RenderContent"/>	
			</fo:block>
		</xsl:for-each>
	</xsl:template>

	<xsl:template match="eaxs:Message">
		<fo:block page-break-after="always"><xsl:call-template name="tag-Art"/>
			<xsl:attribute name="id">MESSAGE_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute>
			<fox:destination><xsl:attribute name="internal-destination">MESSAGE_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute></fox:destination>
			<fo:block xml:lang="en" xsl:use-attribute-sets="h2" padding="0.25em" border="1.5pt solid black"><xsl:call-template name="tag-H2"/><xsl:call-template name="FolderHeader"/> &gt; Message <xsl:value-of select="eaxs:LocalId"/></fo:block>
			<xsl:call-template name="MessageHeaderTocAndContent"/>
			<!-- Create named destination to the end of the message -->
			<fo:inline><xsl:attribute name="id">MESSAGE_END_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute><fox:destination><xsl:attribute name="internal-destination">MESSAGE_END_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute></fox:destination>&nbsp;</fo:inline>
		</fo:block>
	</xsl:template>
	
	<xsl:template name="MessageHeaderTocAndContent">
		<xsl:param name="RenderToc">true</xsl:param>
		<fo:list-block provisional-distance-between-starts="6em" provisional-label-separation="0.25em">
			<xsl:apply-templates select="eaxs:MessageId"/>
			<xsl:apply-templates select="eaxs:OrigDate"/> <!-- MimeKit seems to use this as the default value if there is no date -->
			<xsl:apply-templates select="eaxs:From"/>
			<xsl:apply-templates select="eaxs:Sender"/>
			<xsl:apply-templates select="eaxs:To"/>
			<xsl:apply-templates select="eaxs:Cc"/>
			<xsl:apply-templates select="eaxs:Bcc"/>
			<xsl:apply-templates select="eaxs:Subject"/>
			<xsl:apply-templates select="eaxs:Comments"/>
			<xsl:apply-templates select="eaxs:Keywords"/>
			<xsl:apply-templates select="eaxs:InReplyTo"/>
			<xsl:apply-templates select="eaxs:References[not(../eaxs:InReplyTo = .)]"/><!-- only references which are not already reply to's -->
		</fo:list-block>	
		<xsl:if test="$RenderToc='true'">
			<fo:block xml:lang="en" xsl:use-attribute-sets="h3" border-bottom="1.5pt solid black"><xsl:call-template name="tag-H3"/>Message Contents</fo:block>
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
				<fo:block xml:lang="en">Message Id:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block xml:lang="zxx">
					<xsl:call-template name="InsertZwspAfterNonWords"/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>
	
	<!-- insert zero-width space after non-word characters to facilitate line breaks -->
	<xsl:template name="InsertZwspAfterNonWords">
		<xsl:param name="string" select="."/>
		<xsl:analyze-string select="$string" regex="\W">
			<xsl:matching-substring><xsl:value-of select="."/><xsl:text>&#8203;</xsl:text></xsl:matching-substring>
			<xsl:non-matching-substring><xsl:value-of select="."/></xsl:non-matching-substring>
		</xsl:analyze-string>
	</xsl:template>

	<xsl:template match="eaxs:OrigDate">
		<xsl:if test="fn:year-from-dateTime(.) != 1"> <!-- MimeKit seems to use '0001-01-01T00:00:00Z' as the default value if there is no date -->
			<fo:list-item>
				<fo:list-item-label end-indent="label-end()">
					<fo:block xml:lang="en">Date:</fo:block>
				</fo:list-item-label>
				<fo:list-item-body start-indent="body-start()">
					<fo:block xml:lang="en-us">
						<xsl:value-of select="fn:format-dateTime(., '[FNn], [MNn] [D], [Y], [h]:[m]:[s] [PN]')"/>									
					</fo:block>
				</fo:list-item-body>
			</fo:list-item>
		</xsl:if>
	</xsl:template>

	<xsl:template match="eaxs:From">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block xml:lang="en">From:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block xml:lang="und">
					<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:Sender">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block  xml:lang="en">Sender:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block xml:lang="und">
					<xsl:call-template name="mailbox"/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:To">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block xml:lang="en">To:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block xml:lang="und">
					<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>
	
	<xsl:template match="eaxs:Cc">
		<fo:list-item>
			<fo:list-item-label  end-indent="label-end()">
				<fo:block xml:lang="en">CC:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block xml:lang="und">
					<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>
	
	<xsl:template match="eaxs:Bcc">
		<fo:list-item>
			<fo:list-item-label  end-indent="label-end()">
				<fo:block xml:lang="en">BCC:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block xml:lang="und">
					<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:Subject">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block xml:lang="en">Subject:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block xml:lang="und">
					<xsl:apply-templates/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:Comments">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block xml:lang="en">Comments:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block xml:lang="und">
					<xsl:apply-templates/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:Keywords">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block xml:lang="en">Keywords:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block xml:lang="und">
					<xsl:apply-templates/>
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:InReplyTo">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block xml:lang="en">In Reply To:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block xml:lang="zxx">
					<xsl:call-template name="InsertZwspAfterNonWords"/>						
				</fo:block>
			</fo:list-item-body>
		</fo:list-item>
	</xsl:template>

	<xsl:template match="eaxs:References">
		<fo:list-item>
			<fo:list-item-label end-indent="label-end()">
				<fo:block xml:lang="en">References:</fo:block>
			</fo:list-item-label>
			<fo:list-item-body start-indent="body-start()">
				<fo:block xml:lang="zxx">
					<xsl:call-template name="InsertZwspAfterNonWords"/>
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
				<fo:block xml:lang="und">
					<xsl:apply-templates/>
					<xsl:call-template name="AttachmentLink"/>
					<xsl:if test="not(fn:lower-case(normalize-space(../@IsAttachment)) = 'true') and (fn:lower-case(normalize-space(.)) = 'text/plain' or fn:lower-case(normalize-space(.)) = 'text/html')">
						<fo:basic-link>
							<xsl:attribute name="internal-destination"><xsl:value-of select="fn:generate-id(../eaxs:BodyContent)"/></xsl:attribute>									
							<fo:inline>&nbsp;</fo:inline><fo:inline xsl:use-attribute-sets="a-link" font-size="small">Go To Content</fo:inline>
						</fo:basic-link>												
					</xsl:if>
					<xsl:if test="following-sibling::eaxs:ContentName != following-sibling::eaxs:DispositionFileName or (not(following-sibling::eaxs:DispositionFileName) and following-sibling::eaxs:ContentName)">
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
				<fo:block xml:lang="und">
					<xsl:apply-templates/>
					<xsl:if test="following-sibling::eaxs:DispositionFileName">
						<xsl:text>; f&zwnj;ilename="</xsl:text>
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
				<fo:block xml:lang="und">
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
			<xsl:choose>
				<xsl:when test="count(ancestor::eaxs:Message//eaxs:SingleBody[not(fn:lower-case(normalize-space(@IsAttachment)) = 'true') and starts-with(fn:lower-case(normalize-space(eaxs:ContentType)),'text/')]) > 1 ">
					<!-- Only put the header if there are multiple bodies -->
					<fo:block xml:lang="en" xsl:use-attribute-sets="h3" border-bottom="1.5pt solid black" keep-with-next.within-page="always">
						<xsl:call-template name="tag-H3"/>
						<xsl:attribute name="id"><xsl:value-of select="fn:generate-id(.)"/></xsl:attribute>
						<fox:destination><xsl:attribute name="internal-destination"><xsl:value-of select="fn:generate-id(.)"/></xsl:attribute></fox:destination>
						<xsl:text>Content Type: </xsl:text><xsl:value-of select="../eaxs:ContentType"/>
					</fo:block>				
				</xsl:when>
				<xsl:otherwise>
					<!-- don't put a header, but add an extra line above the content -->
					<fo:block margin-top="1em">
						<xsl:attribute name="id"><xsl:value-of select="fn:generate-id(.)"/></xsl:attribute>
						<fox:destination><xsl:attribute name="internal-destination"><xsl:value-of select="fn:generate-id(.)"/></xsl:attribute></fox:destination>	
						<xsl:text> </xsl:text>
					</fo:block>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:apply-templates select="eaxs:Content | eaxs:ContentAsXhtml"/>
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
			<fo:block xsl:use-attribute-sets="h3"><xsl:call-template name="tag-H3"/>Delivery Status</fo:block>
			<fo:block xsl:use-attribute-sets="h4"><xsl:call-template name="tag-H4"/>Per Message Fields</fo:block>
			<fo:list-block provisional-distance-between-starts="10em" provisional-label-separation="0.25em">
				<xsl:for-each select="eaxs:MessageFields/eaxs:Field">
					<fo:list-item>
						<fo:list-item-label end-indent="label-end()"><fo:block><xsl:value-of select="eaxs:Name"/></fo:block></fo:list-item-label>
						<fo:list-item-body  start-indent="body-start()"><fo:block><xsl:value-of select="eaxs:Value"/></fo:block></fo:list-item-body>
					</fo:list-item>					
				</xsl:for-each>
			</fo:list-block>
			<fo:block xsl:use-attribute-sets="h4"><xsl:call-template name="tag-H4"/>Per Recipient Fields</fo:block>
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
					
					<!-- NOTE:  Using the xml:lang (or language) attribute can interfer with Apache FOP's complex script functionality -->
					<xsl:if test="$fo-processor!='fop'">
						<xsl:choose>
							<xsl:when test="ancestor::*/eaxs:ContentLanguage">
								<xsl:attribute name="xml:lang"><xsl:value-of select="ancestor::*/eaxs:ContentLanguage"/></xsl:attribute>
							</xsl:when>
							<xsl:otherwise>
								<xsl:attribute name="xml:lang">en</xsl:attribute>
							</xsl:otherwise>
						</xsl:choose>
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
				<fo:block><xsl:call-template name="tag-Div"></xsl:call-template>
					
					<!-- NOTE:  Using the xml:lang (or language) attribute can interfer with Apache FOP's complex script functionality -->
					<xsl:if test="$fo-processor!='fop'">
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
							<xsl:when test="ancestor::*/eaxs:ContentLanguage">
								<xsl:attribute name="xml:lang"><xsl:value-of select="ancestor::*/eaxs:ContentLanguage"/></xsl:attribute>
							</xsl:when>
							<xsl:otherwise>
								<xsl:attribute name="xml:lang">en</xsl:attribute>
							</xsl:otherwise>
						</xsl:choose>
						<!-- TODO: Also try to determine xml:lang attribute from html root or html head -->
					</xsl:if>
					
					<xsl:apply-templates select="html:html/html:body/html:*"/>

					<!-- for plain text, add any related inline content below the text -->
					<xsl:if test="ancestor::*[fn:lower-case(normalize-space(eaxs:ContentType)) = 'text/plain'][1] ">
						<xsl:for-each select="ancestor::eaxs:MultiBody[fn:lower-case(normalize-space(eaxs:ContentType))='multipart/related' or ancestor::eaxs:MultiBody[fn:lower-case(normalize-space(eaxs:ContentType))='multipart/mix']]/eaxs:SingleBody">
							<xsl:choose>
								<xsl:when test="fn:starts-with(fn:lower-case(normalize-space(eaxs:ContentType)),'image/')">
									<fo:block>
										<fo:external-graphic xsl:use-attribute-sets="img">
											<xsl:attribute name="src">
												<xsl:call-template name="GetURLForAttachedContent">
													<xsl:with-param name="inline" select="."/>
												</xsl:call-template>
											</xsl:attribute>
											<xsl:attribute name="content-type">content-type:<xsl:value-of select="fn:lower-case(normalize-space(eaxs:ContentType))"/></xsl:attribute>
											<xsl:call-template name="GetImgAltAttr">
												<xsl:with-param name="inline" select="."/>
											</xsl:call-template>
											<xsl:attribute name="overflow">error-if-overflow</xsl:attribute>
											<xsl:choose>
												<xsl:when test="*/eaxs:ImageProperties">
													<xsl:attribute name="width">
														<xsl:value-of select="my:ScaleToBodyWidth(fn:concat(*/eaxs:ImageProperties/eaxs:Width,'px'))"/>
													</xsl:attribute>
													<xsl:attribute name="height">
														<xsl:value-of select="my:ScaleToBodyHeight(fn:concat(*/eaxs:ImageProperties/eaxs:Height,'px'))"/>
													</xsl:attribute>
												</xsl:when>
												<xsl:when test="fn:lower-case(fn:normalize-space($fo-processor))='xep'">
													<!-- XEP crashes if a tall image exceeds the page size, so set the height to a little less than that -->
													<xsl:attribute name="height"><xsl:value-of select="my:PageBodyHeightPts()"/>pt</xsl:attribute>												
												</xsl:when>												
											</xsl:choose>
										</fo:external-graphic>
									</fo:block>
								</xsl:when>
								<xsl:otherwise>
									<fo:block>OTHER: <xsl:value-of select="eaxs:ContentType"/>; <xsl:value-of select="eaxs:ContentName | eaxs:DispositionFileName"/></fo:block>									
									<xsl:message terminate="yes">Unexpected content-type: <xsl:value-of select="eaxs:ContentType"/> for inline attachment</xsl:message>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:for-each>
					</xsl:if>
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


</xsl:stylesheet>
