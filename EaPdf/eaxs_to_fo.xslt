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
	xmlns:fn="http://www.w3.org/2005/xpath-functions"
	xmlns:html="http://www.w3.org/1999/xhtml"
	
	xmlns:pdf="http://xmlgraphics.apache.org/fop/extensions/pdf"
	xmlns:fox="http://xmlgraphics.apache.org/fop/extensions"
	
	xmlns:rx="http://www.renderx.com/XSL/Extensions"
	
	>

	<xsl:import href="eaxs_xhtml2fo.xsl"/>

	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="no" omit-xml-declaration="no"/>
	
	<xsl:param name="SerifFont" select="'Times-Roman'"/>
	<xsl:param name="SansSerifFont" select="'Helvetica'"/>
	<xsl:param name="MonospaceFont" select="'Courier'"/>

	<!-- TGH Which FO Processor -->
	<xsl:param name="fo-processor">fop</xsl:param> <!-- Values used: fop or xep -->
	
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
		<fo:root>
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
				</fo:flow>
			</fo:page-sequence>
		</fo:root>
	</xsl:template>
	
	<xsl:template name="bookmarks">
		<fo:bookmark-tree>
			<xsl:apply-templates select="/eaxs:Account/eaxs:Folder[eaxs:Message]" mode="RenderBookmarks"></xsl:apply-templates>
		</fo:bookmark-tree>
	</xsl:template>
	
	<xsl:template match="eaxs:Folder" mode="RenderBookmarks">
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
					<xsl:apply-templates select="eaxs:Folder[eaxs:Message]" mode="RenderBookmarks"/>
				</fo:bookmark>
			</xsl:if>
		</fo:bookmark>	
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
				<xsl:for-each select="//eaxs:Folder[eaxs:Message]/eaxs:Mbox">
					<pdf:embedded-file>
						<xsl:attribute name="filename"><xsl:value-of select="eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="eaxs:FileExt"/></xsl:attribute>
						<xsl:attribute name="description">Source file for mail folder '<xsl:value-of select="../eaxs:Name"/>'</xsl:attribute>
						<xsl:attribute name="src">url(<xsl:value-of select="fn:resolve-uri(eaxs:RelPath, fn:base-uri())"/>)</xsl:attribute>
					</pdf:embedded-file>				
				</xsl:for-each>
				<!--xsl:for-each select="//eaxs:SingleBody/eaxs:BodyContent[fn:lower-case(normalize-space(eaxs:TransferEncoding)) = 'base64' and (fn:lower-case(normalize-space(../@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(../eaxs:ContentType)),'text/')))]">-->
				<xsl:for-each select="//eaxs:SingleBody/eaxs:BodyContent[fn:lower-case(normalize-space(../@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(../eaxs:ContentType)),'text/'))]">
					<pdf:embedded-file>
						<xsl:attribute name="filename"><xsl:value-of select="eaxs:Hash/eaxs:Value"/>.<xsl:call-template name="GetFileExtension"/></xsl:attribute>
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
				<xsl:for-each select="//eaxs:SingleBody/eaxs:ExtBodyContent">
					<xsl:variable name="rel-path">
						<xsl:call-template name="concat-path">
							<xsl:with-param name="path1" select="normalize-space(ancestor::eaxs:Message/eaxs:RelPath)"/>
							<xsl:with-param name="path2" select="normalize-space(eaxs:RelPath)"/>
						</xsl:call-template>
					</xsl:variable>
					<pdf:embedded-file>
						<xsl:attribute name="filename"><xsl:value-of select="eaxs:Hash/eaxs:Value"/>.<xsl:call-template name="GetFileExtension"/></xsl:attribute>
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
			</fo:declarations>
		</xsl:if>
	</xsl:template>
	
	<xsl:template name="CoverPage">
		<fo:block page-break-after="always">
			<fo:block xsl:use-attribute-sets="h1">PDF Email Archive (PDF/mail-1m)</fo:block>
			<fo:block xsl:use-attribute-sets="h2">
				<xsl:text>Created: </xsl:text>
				<xsl:value-of select="fn:format-dateTime(fn:current-dateTime(), '[FNn], [MNn] [D], [Y], [h]:[m]:[s] [PN]')"/>
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
			
			<!-- TODO: Do not count child messages -->
			<fo:block xsl:use-attribute-sets="h2">Message Count: <xsl:value-of select="count(//eaxs:Message)"/></fo:block>
			<!-- TODO: Only count distinct attachments, based on the hash -->
			<fo:block xsl:use-attribute-sets="h2">Attachment Count: <xsl:value-of select="count(//eaxs:SingleBody[(eaxs:ExtBodyContent or fn:lower-case(normalize-space(eaxs:BodyContent/eaxs:TransferEncoding)) = 'base64') and (fn:lower-case(normalize-space(@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(eaxs:ContentType)),'text/')))])"/></fo:block>
			
			<xsl:choose>
				<xsl:when test="count(//eaxs:Folder[eaxs:Message]) > 1">
					<fo:block xsl:use-attribute-sets="h2">Folders: <xsl:value-of select="count(/eaxs:Account//eaxs:Folder[eaxs:Message])"/></fo:block>
					<xsl:apply-templates select="/eaxs:Account/eaxs:Folder[eaxs:Message]" mode="RenderToc"/>
				</xsl:when>	
				<xsl:when test="count(//eaxs:Folder[eaxs:Message]) = 1">
					<fo:block xsl:use-attribute-sets="h2">
						Folder: <xsl:value-of select="/eaxs:Account/eaxs:Folder[eaxs:Message]/eaxs:Name"/>
						<xsl:choose>
							<xsl:when test="$fo-processor='fop'">
								<fo:basic-link>
									<xsl:attribute name="external-destination">url(embedded-file:<xsl:value-of select="/eaxs:Account/eaxs:Folder[eaxs:Message]/eaxs:Mbox/eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="/eaxs:Account/eaxs:Folder[eaxs:Message]/eaxs:Mbox/eaxs:FileExt"/>)</xsl:attribute>
									<fo:inline font-size="small"> (<fo:inline xsl:use-attribute-sets="a-link" >Open Source File</fo:inline>)</fo:inline>
								</fo:basic-link>
							</xsl:when>
							<xsl:when test="$fo-processor='xep'">
								<fo:inline font-size="small"> (Open Source File) &nbsp;&nbsp;
									<rx:pdf-comment>
										<xsl:attribute name="title">Source File &mdash; <xsl:value-of select="/eaxs:Account/eaxs:Folder/eaxs:Mbox/eaxs:RelPath"/></xsl:attribute>
										<rx:pdf-file-attachment icon-type="pushpin">
											<xsl:attribute name="filename"><xsl:value-of select="/eaxs:Account/eaxs:Folder/eaxs:Mbox/eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="/eaxs:Account/eaxs:Folder/eaxs:Mbox/eaxs:FileExt"/></xsl:attribute>
											<xsl:attribute name="src">url(<xsl:value-of select="fn:resolve-uri(/eaxs:Account/eaxs:Folder/eaxs:Mbox/eaxs:RelPath, fn:base-uri())"/>)</xsl:attribute>
										</rx:pdf-file-attachment>
									</rx:pdf-comment>
								</fo:inline>
							</xsl:when>
						</xsl:choose>
					</fo:block>					
				</xsl:when>
			</xsl:choose>
		</fo:block>
	</xsl:template>
	
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
								<fo:basic-link>
									<xsl:attribute name="external-destination">url(embedded-file:<xsl:value-of select="eaxs:Mbox/eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="eaxs:Mbox/eaxs:FileExt"/>)</xsl:attribute>
									<fo:inline font-size="small"> (<fo:inline xsl:use-attribute-sets="a-link" >Open Source File</fo:inline>)</fo:inline>
								</fo:basic-link>
							</xsl:when>
							<xsl:when test="$fo-processor='xep'">
								<fo:inline font-size="small"> (Open Source File) &nbsp;&nbsp;
									<rx:pdf-comment>
										<xsl:attribute name="title">Source File &mdash; <xsl:value-of select="eaxs:Mbox/eaxs:RelPath"/></xsl:attribute>
										<rx:pdf-file-attachment icon-type="pushpin">
											<xsl:attribute name="filename"><xsl:value-of select="eaxs:Mbox/eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="eaxs:Mbox/eaxs:FileExt"/></xsl:attribute>
											<xsl:attribute name="src">url(<xsl:value-of select="fn:resolve-uri(eaxs:Mbox/eaxs:RelPath, fn:base-uri())"/>)</xsl:attribute>
										</rx:pdf-file-attachment>
									</rx:pdf-comment>
								</fo:inline>
							</xsl:when>
						</xsl:choose>
					</fo:block>
					<xsl:apply-templates select="eaxs:Folder[eaxs:Message]" mode="RenderToc"/>
				</fo:list-item-body>
			</fo:list-item>
		</fo:list-block>
	</xsl:template>
	
	<xsl:template match="eaxs:Folder" mode="RenderContent">
		<fo:block>
			<xsl:attribute name="id"><xsl:value-of select="generate-id(.)"/></xsl:attribute>
			<xsl:apply-templates select="eaxs:Message" />	
			<xsl:apply-templates select="eaxs:Folder" mode="RenderContent"/>	
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:Message">
		<fo:block page-break-after="always">
			<xsl:attribute name="id">MESSAGE_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute>
			<fo:block xsl:use-attribute-sets="h3" padding="0.25em" border="1.5pt solid black"><xsl:call-template name="FolderHeader"/> &gt; Message <xsl:value-of select="eaxs:LocalId"/></fo:block>
			<xsl:call-template name="MessageHeaderTocAndContent"/>
		</fo:block>
	</xsl:template>
	
	<xsl:template name="MessageHeaderTocAndContent">
		<xsl:message><xsl:value-of select="parent::eaxs:Folder/eaxs:Mbox/eaxs:RelPath"/> -- <xsl:value-of select="eaxs:MessageId"/></xsl:message>
			<fo:list-block provisional-distance-between-starts="6em" provisional-label-separation="0.25em">
				<xsl:apply-templates select="eaxs:MessageId"/>
				<xsl:apply-templates select="eaxs:OrigDate"/>
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
		<fo:block xsl:use-attribute-sets="h3" border-bottom="1.5pt solid black">Message Contents</fo:block>
		<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody" mode="RenderToc"/>
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
		<xsl:apply-templates select="eaxs:BodyContent | eaxs:ExtBodyContent | eaxs:ChildMessage | eaxs:DeliveryStatus"/>
	</xsl:template>
	
	<xsl:template match="eaxs:BodyContent">
		<xsl:if test="not(fn:lower-case(normalize-space(../@IsAttachment)) = 'true') and starts-with(fn:lower-case(normalize-space(../eaxs:ContentType)),'text/')">
			<!-- only render content which is text and which is not an attachment -->
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
			<!-- TODO:  Instead of rendering the full Message Header, Toc, and Content, just render the Message Header and Content, not the TOC -->
			<xsl:call-template name="MessageHeaderTocAndContent"></xsl:call-template>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="eaxs:DeliveryStatus">
		<fo:block xsl:use-attribute-sets="delivery-status">
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
					<xsl:call-template name="process-pre"/>
				</fo:block>			
			</xsl:when>	
			<xsl:otherwise>
				<fo:inline font-style="italic">BLANK</fo:inline>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	<xsl:template match="eaxs:ContentAsXhtml">
		<xsl:choose>
			<xsl:when test="not(normalize-space(.) = '')">
				<xsl:apply-templates select="html:html/html:body/html:*"/>
			</xsl:when>	
			<xsl:otherwise>
				<fo:inline font-style="italic">BLANK</fo:inline>
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
					<fo:basic-link>
						<xsl:attribute name="external-destination">url(embedded-file:<xsl:value-of select="$single-body/eaxs:*/eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="$file-ext"/>)</xsl:attribute>
						<fo:inline> (<fo:inline xsl:use-attribute-sets="a-link" >Open Attachment</fo:inline>)</fo:inline>
					</fo:basic-link>												
				</xsl:when>
				<xsl:when test="$fo-processor='xep'">
					<fo:inline font-size="small"> (Open Attachment) &nbsp;&nbsp;
						<rx:pdf-comment>
							<xsl:attribute name="title">Attachment &mdash; </xsl:attribute>
							<rx:pdf-file-attachment icon-type="pushpin">
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

</xsl:stylesheet>
