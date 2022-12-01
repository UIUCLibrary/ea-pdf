<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
				xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				xmlns:msxsl="urn:schemas-microsoft-com:xslt" 
				xmlns:fo="http://www.w3.org/1999/XSL/Format"
				xmlns:fox="http://xmlgraphics.apache.org/fop/extensions"
				xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"
				exclude-result-prefixes="msxsl">
	
	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes" omit-xml-declaration="no" />

	<xsl:param name="SerifFont" select="'Times-Roman'"/>
	<xsl:param name="SansSerifFont" select="'Helvetica'"/>
	<xsl:param name="MonospaceFont" select="'Courier'"/>

	<xsl:template match="/">
		<fo:root>
			<fo:layout-master-set>
				<fo:simple-page-master master-name="message-page" page-width="8.5in" page-height="11in"  >
					<fo:region-body margin-top="1in" margin-bottom="1in" margin-left="1in" margin-right="1in" />
					<fo:region-before extent="1in"/>
					<fo:region-after extent="1in"/>
				</fo:simple-page-master>
			</fo:layout-master-set>
			<fo:page-sequence master-reference="message-page">
				<fo:static-content flow-name="xsl-region-before">
					<fo:block text-align="center" margin-left="1in" margin-right="1in">Account: <xsl:value-of select="/eaxs:Account/eaxs:EmailAddress[1]"/> Folder: <xsl:value-of select="/eaxs:Account/eaxs:Folder/eaxs:Name"/></fo:block>
				</fo:static-content>
				<fo:static-content flow-name="xsl-region-after">
					<fo:block text-align="center" margin-left="1in" margin-right="1in"><fo:page-number/></fo:block>
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
				<fo:leader leader-pattern="rule" leader-length="100%" rule-style="solid" rule-thickness="1.5pt"/>
			</fo:block>				
			<xsl:apply-templates select="eaxs:SingleBody|eaxs:MultiBody">
				<xsl:with-param name="depth" select="0"/>
			</xsl:apply-templates>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="eaxs:MessageId">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block white-space-collapse="false" linefeed-treatment="preserve" >Message&#160;Id: </fo:block></fo:inline-container><xsl:apply-templates/>
		</fo:block>		
	</xsl:template>
	
	<xsl:template match="eaxs:OrigDate">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block white-space-collapse="false" linefeed-treatment="preserve" >Date: </fo:block></fo:inline-container><xsl:value-of select="msxsl:format-date(., 'dddd, MMM dd, yyyy, ')"/> <xsl:value-of select="msxsl:format-time(., ' h:m:s tt')"/>
		</fo:block>		
	</xsl:template>

	<xsl:template match="eaxs:From">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block>From:</fo:block></fo:inline-container><xsl:apply-templates select="eaxs:Mailbox|eaxs:Group"/>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="eaxs:Sender">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block>Sender:</fo:block></fo:inline-container><xsl:call-template name="mailbox" />
		</fo:block>
	</xsl:template>
	
	<xsl:template match="eaxs:To">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block>To:</fo:block></fo:inline-container><xsl:apply-templates select="eaxs:Mailbox|eaxs:Group"/>
		</fo:block>
	</xsl:template>
	<xsl:template match="eaxs:Cc">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block>CC:</fo:block></fo:inline-container><xsl:apply-templates select="eaxs:Mailbox|eaxs:Group"/>
		</fo:block>
	</xsl:template>
	<xsl:template match="eaxs:Bcc">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block>BCC:</fo:block></fo:inline-container><xsl:apply-templates select="eaxs:Mailbox|eaxs:Group"/>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="eaxs:Subject">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block white-space-collapse="false" linefeed-treatment="preserve" >Subject: </fo:block></fo:inline-container><xsl:apply-templates/>
		</fo:block>		
	</xsl:template>
	
	<xsl:template match="eaxs:Comments">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block white-space-collapse="false" linefeed-treatment="preserve" >Comments: </fo:block></fo:inline-container><xsl:apply-templates/>
		</fo:block>		
	</xsl:template>

	<xsl:template match="eaxs:Keywords">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block white-space-collapse="false" linefeed-treatment="preserve" >Keywords: </fo:block></fo:inline-container><xsl:apply-templates/>
		</fo:block>		
	</xsl:template>
	
	<xsl:template match="eaxs:InReplyTo">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block white-space-collapse="false" linefeed-treatment="preserve" >In&#160;Reply&#160;To: </fo:block></fo:inline-container><xsl:apply-templates/>
		</fo:block>		
	</xsl:template>

	<xsl:template match="eaxs:Mailbox">
		<xsl:call-template name="mailbox"/>
		<xsl:if test="position() != last()">, </xsl:if>
	</xsl:template>
	
	<xsl:template match="eaxs:Group">
		<fo:inline font-weight="bold"><xsl:value-of select="eaxs:Name"/> [</fo:inline><xsl:apply-templates select="eaxs:Mailbox|eaxs:Group"/><fo:inline font-weight="bold"><xsl:value-of select="eaxs:Name"/>] </fo:inline>
	</xsl:template>
	
	<xsl:template name="mailbox">
		<!-- TODO: Add the @name -->
		<xsl:value-of select="@address"/>
	</xsl:template>
	
	<xsl:attribute-set name="hanging-indent-6em">
		<xsl:attribute name="start-indent">6em</xsl:attribute>
		<xsl:attribute name="text-indent">-6em</xsl:attribute>
	</xsl:attribute-set>
	
	<xsl:template match="eaxs:SingleBody">
		<xsl:param name="depth" select="0"/>
		<fo:block>
			<xsl:attribute name="start-indent"><xsl:value-of select="$depth"/>em</xsl:attribute>
			<xsl:apply-templates select="eaxs:ContentType"/>
			<xsl:apply-templates select="eaxs:Disposition"/>
			<xsl:apply-templates select="eaxs:ContentLanguage"/>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="eaxs:MultiBody">
		<xsl:param name="depth" select="0"/>		
		<fo:block>
			<xsl:attribute name="start-indent"><xsl:value-of select="$depth"/>em</xsl:attribute>
			<xsl:apply-templates select="eaxs:ContentType"/>
			<xsl:apply-templates select="eaxs:Disposition"/>
			<xsl:apply-templates select="eaxs:ContentLanguage"/>
			
			<xsl:apply-templates select="eaxs:SingleBody|eaxs:MultiBody">
				<xsl:with-param name="depth" select="$depth + 6"/>
			</xsl:apply-templates>
		</fo:block>
	</xsl:template>

	<xsl:template match="eaxs:ContentType">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block white-space-collapse="false" linefeed-treatment="preserve" >Content&#160;Type:</fo:block></fo:inline-container><xsl:apply-templates/>
		</fo:block>		
	</xsl:template>

	<xsl:template match="eaxs:Disposition">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block white-space-collapse="false" linefeed-treatment="preserve" >Disposition:</fo:block></fo:inline-container><xsl:apply-templates/>
		</fo:block>		
	</xsl:template>
	
	<xsl:template match="eaxs:ContentLanguage">
		<fo:block xsl:use-attribute-sets="hanging-indent-6em">
			<fo:inline-container width="6em"><fo:block white-space-collapse="false" linefeed-treatment="preserve" >Content&#160;Language:</fo:block></fo:inline-container><xsl:apply-templates/>
		</fo:block>		
	</xsl:template>

</xsl:stylesheet>
