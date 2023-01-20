<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE stylesheet
[
<!ENTITY mdash "&#8212;" >
<!ENTITY nbsp "&#160;" >
]>

<xsl:stylesheet version="2.0" 
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:fo="http://www.w3.org/1999/XSL/Format"
	xmlns:pdf="http://xmlgraphics.apache.org/fop/extensions/pdf"
	xmlns:fn="http://www.w3.org/2005/xpath-functions"
	xmlns:fox="http://xmlgraphics.apache.org/fop/extensions"
	xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"
	xmlns:html="http://www.w3.org/1999/xhtml"
	>

	<xsl:import href="eaxs_xhtml2fo.xsl"/>

	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="no" omit-xml-declaration="no"/>
	
	<xsl:param name="SerifFont" select="'Times-Roman'"/>
	<xsl:param name="SansSerifFont" select="'Helvetica'"/>
	<xsl:param name="MonospaceFont" select="'Courier'"/>

	<xsl:template match="/">
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
					<!-- TODO: Need to add a support for multiple folders, possibly nested, in the file -->
					<!--<xsl:apply-templates select="/eaxs:Account/eaxs:Folder/eaxs:Message" />-->
				</fo:flow>
			</fo:page-sequence>
		</fo:root>
	</xsl:template>
	
	<xsl:template name="declarations">
		<fo:declarations>
			<xsl:for-each select="//eaxs:Folder[eaxs:Message]/eaxs:Mbox">
				<pdf:embedded-file>
					<xsl:attribute name="filename"><xsl:value-of select="eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="eaxs:FileExt"/></xsl:attribute>
					<xsl:attribute name="src">url(<xsl:value-of select="fn:resolve-uri(eaxs:RelPath, fn:base-uri())"/>)</xsl:attribute>
					<xsl:attribute name="description">Source file for mail folder '<xsl:value-of select="../eaxs:Name"/>'</xsl:attribute>
				</pdf:embedded-file>				
			</xsl:for-each>
			<xsl:for-each select="//eaxs:SingleBody[fn:lower-case(normalize-space(eaxs:BodyContent/eaxs:TransferEncoding)) = 'base64' and (fn:lower-case(normalize-space(@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(eaxs:ContentType)),'text/')))]">
				<pdf:embedded-file>
					<xsl:attribute name="filename"><xsl:value-of select="eaxs:BodyContent/eaxs:Hash/eaxs:Value"/>.<xsl:call-template name="GetFileExtension"/></xsl:attribute>
					<xsl:attribute name="src">url('data:<xsl:value-of select="fn:normalize-space(fn:lower-case(eaxs:ContentType))"/>;base64,<xsl:value-of select="eaxs:BodyContent/eaxs:Content"/>')</xsl:attribute>
					<xsl:attribute name="description">Original File Name: <xsl:value-of select="eaxs:DispositionFile | eaxs:ContentName"/></xsl:attribute>
				</pdf:embedded-file>								
			</xsl:for-each>
		</fo:declarations>
	</xsl:template>
	
	<xsl:template name="CoverPage">
		<fo:block page-break-after="always">
			<fo:block xsl:use-attribute-sets="h1">PDF Email Archive (PDF/mail-m)</fo:block>
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
			<fo:block xsl:use-attribute-sets="h2">Attachment Count: <xsl:value-of select="count(//eaxs:SingleBody[fn:lower-case(normalize-space(eaxs:BodyContent/eaxs:TransferEncoding)) = 'base64' and (fn:lower-case(normalize-space(@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(eaxs:ContentType)),'text/')))])"/></fo:block>
			
			<xsl:choose>
				<xsl:when test="count(//eaxs:Folder[eaxs:Message]) > 1">
					<fo:block xsl:use-attribute-sets="h2">Folders: <xsl:value-of select="count(/eaxs:Account//eaxs:Folder[eaxs:Message])"/></fo:block>
					<xsl:apply-templates select="/eaxs:Account/eaxs:Folder[eaxs:Message]"/>
				</xsl:when>	
				<xsl:when test="count(//eaxs:Folder[eaxs:Message]) = 1">
					<fo:block xsl:use-attribute-sets="h2">
						Folder: <xsl:value-of select="/eaxs:Account/eaxs:Folder[eaxs:Message]/eaxs:Name"/>
						<fo:basic-link>
							<xsl:attribute name="external-destination">url(embedded-file:<xsl:value-of select="/eaxs:Account/eaxs:Folder[eaxs:Message]/eaxs:Mbox/eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="/eaxs:Account/eaxs:Folder[eaxs:Message]/eaxs:Mbox/eaxs:FileExt"/>)</xsl:attribute>
							<fo:inline font-size="small"> (<fo:inline xsl:use-attribute-sets="a-link" >Open Source File</fo:inline>)</fo:inline>
						</fo:basic-link>
					</fo:block>					
				</xsl:when>
			</xsl:choose>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="eaxs:Folder">
		<fo:list-block>
			<fo:list-item>
				<fo:list-item-label end-indent="label-end()"><fo:block xsl:use-attribute-sets="h3">&#x2022;</fo:block></fo:list-item-label>
				<fo:list-item-body start-indent="body-start()">
					<fo:block xsl:use-attribute-sets="h3">
						<xsl:apply-templates select="eaxs:Name"/>
						<fo:inline font-size="small"> (<xsl:value-of select="count(eaxs:Message)"/> Messages)</fo:inline>
						<fo:basic-link>
							<xsl:attribute name="external-destination">url(embedded-file:<xsl:value-of select="eaxs:Mbox/eaxs:Hash/eaxs:Value"/>.<xsl:value-of select="eaxs:Mbox/eaxs:FileExt"/>)</xsl:attribute>
							<fo:inline font-size="small"> (<fo:inline xsl:use-attribute-sets="a-link" >Open Source File</fo:inline>)</fo:inline>
						</fo:basic-link>
					</fo:block>
					<xsl:apply-templates select="eaxs:Folder[eaxs:Message]"/>
				</fo:list-item-body>
			</fo:list-item>
		</fo:list-block>
	</xsl:template>

	<xsl:template match="eaxs:Message">
		<fo:block page-break-after="always">
			<fo:block xsl:use-attribute-sets="h2" padding="0.25em" border="1.5pt solid black">Message <xsl:value-of select="eaxs:LocalId"/></fo:block>
			<xsl:call-template name="Message"/>
		</fo:block>
	</xsl:template>
	
	<xsl:template name="Message">
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
		<fo:block xsl:use-attribute-sets="h3" border-bottom="1.5pt solid black">Message Contents</fo:block>
		<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody" mode="RenderToc"/>
		<xsl:call-template name="hr"/>
		<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody" mode="RenderContent"/>
	</xsl:template>
	
	<xsl:template match="eaxs:MessageId">
		<fo:block xsl:use-attribute-sets="hanging-indent-header1">
			<fo:inline-container xsl:use-attribute-sets="width-header1">
				<fo:block keep-together="always">Message Id:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates/>
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:OrigDate">
		<fo:block xsl:use-attribute-sets="hanging-indent-header1">
			<fo:inline-container xsl:use-attribute-sets="width-header1">
				<fo:block keep-together="always">Date:</fo:block>
			</fo:inline-container>
			<xsl:value-of select="fn:format-dateTime(., '[FNn], [MNn] [D], [Y], [h]:[m]:[s] [PN]')"/>
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:From">
		<fo:block xsl:use-attribute-sets="hanging-indent-header1">
			<fo:inline-container xsl:use-attribute-sets="width-header1">
				<fo:block keep-together="always">From:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:Sender">
		<fo:block xsl:use-attribute-sets="hanging-indent-header1">
			<fo:inline-container xsl:use-attribute-sets="width-header1">
				<fo:block keep-together="always">Sender:</fo:block>
			</fo:inline-container>
			<xsl:call-template name="mailbox"/>
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:To">
		<fo:block xsl:use-attribute-sets="hanging-indent-header1">
			<fo:inline-container xsl:use-attribute-sets="width-header1">
				<fo:block keep-together="always">To:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
		</fo:block>
	</xsl:template>
	<xsl:template match="eaxs:Cc">
		<fo:block xsl:use-attribute-sets="hanging-indent-header1">
			<fo:inline-container xsl:use-attribute-sets="width-header1">
				<fo:block keep-together="always">CC:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
		</fo:block>
	</xsl:template>
	<xsl:template match="eaxs:Bcc">
		<fo:block xsl:use-attribute-sets="hanging-indent-header1">
			<fo:inline-container xsl:use-attribute-sets="width-header1">
				<fo:block keep-together="always">BCC:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates select="eaxs:Mailbox | eaxs:Group"/>
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:Subject">
		<fo:block xsl:use-attribute-sets="hanging-indent-header1">
			<fo:inline-container xsl:use-attribute-sets="width-header1">
				<fo:block keep-together="always">Subject:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates/>
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:Comments">
		<fo:block xsl:use-attribute-sets="hanging-indent-header1">
			<fo:inline-container xsl:use-attribute-sets="width-header1">
				<fo:block keep-together="always">Comments:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates/>
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:Keywords">
		<fo:block xsl:use-attribute-sets="hanging-indent-header1">
			<fo:inline-container xsl:use-attribute-sets="width-header1">
				<fo:block keep-together="always">Keywords:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates/>
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:InReplyTo">
		<fo:block xsl:use-attribute-sets="hanging-indent-header1">
			<fo:inline-container xsl:use-attribute-sets="width-header1">
				<fo:block keep-together="always">In Reply To:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates/>
		</fo:block>
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
		<!-- TODO: make these clickable to go to rendered content or to download attachments -->
		<xsl:param name="depth" select="0"/>
		<fo:block-container border-left="1px solid black" margin-top="5px">
			<xsl:attribute name="margin-left"><xsl:value-of select="$depth"/>em</xsl:attribute>
			<xsl:apply-templates select="eaxs:ContentType"/>
			<xsl:apply-templates select="eaxs:Disposition"/>
			<xsl:apply-templates select="eaxs:ContentLanguage"/>
		</fo:block-container>
	</xsl:template>

	<xsl:template match="eaxs:MultiBody" mode="RenderToc">
		<xsl:param name="depth" select="0"/>
		<fo:block-container border-left="1px solid black" margin-top="5px">
			<xsl:attribute name="margin-left"><xsl:value-of select="$depth"/>em</xsl:attribute>
			<xsl:apply-templates select="eaxs:ContentType"/>
			<xsl:apply-templates select="eaxs:Disposition"/>
			<xsl:apply-templates select="eaxs:ContentLanguage"/>

			<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody" mode="RenderToc">
				<xsl:with-param name="depth" select="1"/>
			</xsl:apply-templates>
		</fo:block-container>
	</xsl:template>

	<xsl:template match="eaxs:ContentType">
		<fo:block xsl:use-attribute-sets="hanging-indent-header2">
			<fo:inline-container xsl:use-attribute-sets="width-header2">
				<fo:block keep-together="always">Content Type:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates/>
			<xsl:if test="fn:lower-case(normalize-space(../eaxs:BodyContent/eaxs:TransferEncoding)) = 'base64' and (fn:lower-case(normalize-space(../@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(.)),'text/')))">
				<fo:basic-link>
					<xsl:attribute name="external-destination">url(embedded-file:<xsl:value-of select="../eaxs:BodyContent/eaxs:Hash/eaxs:Value"/>.<xsl:call-template name="GetFileExtension"><xsl:with-param name="SingleBody" select=".."></xsl:with-param></xsl:call-template>)</xsl:attribute>
					<fo:inline> (<fo:inline xsl:use-attribute-sets="a-link" >Open Attachment</fo:inline>)</fo:inline>
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
	</xsl:template>

	<xsl:template match="eaxs:Disposition">
		<fo:block xsl:use-attribute-sets="hanging-indent-header2">
			<fo:inline-container xsl:use-attribute-sets="width-header2">
				<fo:block keep-together="always">Disposition:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates/>
			<xsl:if test="following-sibling::eaxs:DispositionFileName">
				<xsl:text>; filename="</xsl:text>
				<xsl:call-template name="escape-specials">
					<xsl:with-param name="text"><xsl:value-of select="following-sibling::eaxs:DispositionFileName"/></xsl:with-param>
				</xsl:call-template>
				<xsl:text>"</xsl:text>
			</xsl:if>
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:ContentLanguage">
		<fo:block xsl:use-attribute-sets="hanging-indent-header2">
			<fo:inline-container xsl:use-attribute-sets="width-header2">
				<fo:block keep-together="always">Content Language:</fo:block>
			</fo:inline-container>
			<xsl:apply-templates/>
		</fo:block>
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
		<fo:block xsl:use-attribute-sets="todo">TODO: ExtBodyContent</fo:block>
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
			<xsl:call-template name="Message"></xsl:call-template>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="eaxs:DeliveryStatus">
		<fo:block xsl:use-attribute-sets="todo">TODO: DeliveryStatus</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:Content">
		<xsl:choose>
			<xsl:when test="not(normalize-space(.) = '')">
				<fo:block xsl:use-attribute-sets="todo">TODO: Content</fo:block>			
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
	
	<xsl:attribute-set name="todo">
		<xsl:attribute name="color">red</xsl:attribute>
		<xsl:attribute name="font-size">large</xsl:attribute>
		<xsl:attribute name="font-weight">bold</xsl:attribute>
		<xsl:attribute name="margin-top">1em</xsl:attribute>
	</xsl:attribute-set>

	<xsl:attribute-set name="hanging-indent-header1">
		<xsl:attribute name="margin-left">6em</xsl:attribute>
		<xsl:attribute name="text-indent">-6em</xsl:attribute>
	</xsl:attribute-set>
	<xsl:attribute-set name="width-header1">
		<xsl:attribute name="width">6em</xsl:attribute>
	</xsl:attribute-set>

	<xsl:attribute-set name="hanging-indent-header2">
		<xsl:attribute name="margin-left">10em</xsl:attribute>
		<xsl:attribute name="text-indent">-10em</xsl:attribute>
	</xsl:attribute-set>
	<xsl:attribute-set name="width-header2">
		<xsl:attribute name="width">10em</xsl:attribute>
	</xsl:attribute-set>

	<!-- ========================================================================
		Some utility templates 
	============================================================================= -->
	
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
		<xsl:param name="SingleBody" select="."/>
		<xsl:variable name="ContentName" select="fn:tokenize($SingleBody/eaxs:ContentName,'\.')[last()]"/>
		<xsl:variable name="DispositionFilename" select="fn:tokenize($SingleBody/eaxs:DispositionFilename,'\.')[last()]"/>
		<xsl:choose>
			<xsl:when test="$DispositionFilename"><xsl:value-of select="$DispositionFilename"/></xsl:when>
			<xsl:when test="$ContentName"><xsl:value-of select="$ContentName"/></xsl:when>
			<xsl:otherwise>bin</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
</xsl:stylesheet>
