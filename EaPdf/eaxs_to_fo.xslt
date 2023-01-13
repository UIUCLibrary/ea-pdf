<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE stylesheet
[
<!ENTITY mdash "&#8212;" >
<!ENTITY nbsp "&#160;" >
]>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:fo="http://www.w3.org/1999/XSL/Format"
	xmlns:fox="http://xmlgraphics.apache.org/fop/extensions"
	xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"
	xmlns:html="http://www.w3.org/1999/xhtml"
	exclude-result-prefixes="msxsl">

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
					<xsl:apply-templates select="/eaxs:Account/eaxs:Folder/eaxs:Message"/>
				</fo:flow>
			</fo:page-sequence>
		</fo:root>
	</xsl:template>

	<xsl:template match="eaxs:Message">
		<fo:block page-break-after="always">
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
			<fo:block>
				<fo:leader leader-pattern="rule" leader-length="100%" rule-style="solid"
					rule-thickness="1.5pt"/>
			</fo:block>
			<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody"/>
			<fo:block>
				<fo:leader leader-pattern="rule" leader-length="100%" rule-style="solid"
					rule-thickness="1.5pt"/>
			</fo:block>
			<xsl:apply-templates select=".//eaxs:SingleBody[@IsAttachment='false' and starts-with(eaxs:ContentType,'text/')]/eaxs:BodyContent"/>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="eaxs:BodyContent">
		<xsl:apply-templates select="eaxs:Content | eaxs:ContentAsXhtml"/>
		<xsl:if test="position() != last()">
			<fo:block>
				<fo:leader leader-pattern="rule" leader-length="100%" rule-style="solid"
					rule-thickness="1.5pt"/>
			</fo:block>			
		</xsl:if>
	</xsl:template>
	
	<xsl:template match="eaxs:ContentAsXhtml">
		<xsl:apply-templates select="html:html/html:body/html:*"/>
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
			<xsl:choose>
				<xsl:when test="function-available('msxsl:format-date')">
					<xsl:value-of select="msxsl:format-date(., 'dddd, MMM dd, yyyy, ')"/>
					<xsl:value-of select="msxsl:format-time(., ' h:m:s tt')"/>					
				</xsl:when>
				<xsl:when test="function-available('format-dateTime')">
					<xsl:value-of select="format-dateTime(., '[FNn], [MNn] [D], [Y], [h]:[m]:[s] [PN]')"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="."/>
				</xsl:otherwise>
			</xsl:choose> 
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

	<xsl:template match="eaxs:SingleBody">
		<xsl:param name="depth" select="0"/>
		<fo:block-container border-left="1px solid black" margin-top="5px">
			<xsl:attribute name="margin-left"><xsl:value-of select="$depth"/>em</xsl:attribute>
			<xsl:apply-templates select="eaxs:ContentType"/>
			<xsl:apply-templates select="eaxs:Disposition"/>
			<xsl:apply-templates select="eaxs:ContentLanguage"/>
		</fo:block-container>
	</xsl:template>

	<xsl:template match="eaxs:MultiBody">
		<xsl:param name="depth" select="0"/>
		<fo:block-container border-left="1px solid black" margin-top="5px">
			<xsl:attribute name="margin-left"><xsl:value-of select="$depth"/>em</xsl:attribute>
			<xsl:apply-templates select="eaxs:ContentType"/>
			<xsl:apply-templates select="eaxs:Disposition"/>
			<xsl:apply-templates select="eaxs:ContentLanguage"/>

			<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody">
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
			<xsl:if test="following-sibling::eaxs:Charset">
				<xsl:text>; charset=</xsl:text><xsl:value-of select="following-sibling::eaxs:Charset"/>
			</xsl:if>
			<xsl:if test="following-sibling::eaxs:ContentName">
				<xsl:text>; name="</xsl:text>
				<xsl:call-template name="escape-specials">
					<xsl:with-param name="text"><xsl:value-of select="following-sibling::eaxs:ContentName"/></xsl:with-param>
				</xsl:call-template>
				<xsl:text>"</xsl:text>
			</xsl:if>
			<xsl:for-each select="following-sibling::eaxs:ContentTypeParam">
				<xsl:text>; </xsl:text>
				<xsl:value-of select="eaxs:Name"/>
				<xsl:text>="</xsl:text>
				<xsl:call-template name="escape-specials">
					<xsl:with-param name="text"><xsl:value-of select="eaxs:Value"/></xsl:with-param>
				</xsl:call-template>
				<xsl:text>"</xsl:text>
			</xsl:for-each>
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
			<xsl:for-each select="following-sibling::eaxs:DispositionParam">
				<xsl:text>; </xsl:text>
				<xsl:value-of select="eaxs:Name"/>
				<xsl:text>="</xsl:text>
				<xsl:call-template name="escape-specials">
					<xsl:with-param name="text"><xsl:value-of select="eaxs:Value"/></xsl:with-param>
				</xsl:call-template>
				<xsl:text>"</xsl:text>
			</xsl:for-each>
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

	<!-- ========================================================================
		Attribute Sets 
	============================================================================= -->

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
	

</xsl:stylesheet>
