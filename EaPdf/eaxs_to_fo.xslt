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
			<xsl:apply-templates select="eaxs:LocalId"></xsl:apply-templates>
			<xsl:apply-templates select="eaxs:OrigDate"></xsl:apply-templates>
			<xsl:apply-templates select="eaxs:Subject"></xsl:apply-templates>
			<xsl:apply-templates select="eaxs:From"></xsl:apply-templates>
			<xsl:if test="eaxs:To">
				<fo:block>
					<fo:leader leader-pattern="rule" leader-length="100%" rule-style="solid" rule-thickness="1.5pt"/>
				</fo:block>				
			</xsl:if>
			<xsl:apply-templates select="eaxs:To"></xsl:apply-templates>
			<xsl:if test="eaxs:Cc">
				<fo:block>
					<fo:leader leader-pattern="rule" leader-length="100%" rule-style="solid" rule-thickness="1.5pt"/>
				</fo:block>				
			</xsl:if>
			<xsl:apply-templates select="eaxs:Cc"></xsl:apply-templates>
			<xsl:if test="eaxs:Bcc">
				<fo:block>
					<fo:leader leader-pattern="rule" leader-length="100%" rule-style="solid" rule-thickness="1.5pt"/>
				</fo:block>				
			</xsl:if>
			<xsl:apply-templates select="eaxs:Bcc"></xsl:apply-templates>
			<xsl:apply-templates select=".//eaxs:SingleBody[@IsAttachment='false' and eaxs:ContentType='text/plain'][1]/eaxs:BodyContent/eaxs:Content"></xsl:apply-templates>
		</fo:block>
	</xsl:template>
	
	<xsl:template match="eaxs:LocalId">
		<fo:block font-weight="bold" white-space-collapse="false" linefeed-treatment="preserve" space-after="6pt" space-before="6pt">Message: <xsl:apply-templates/></fo:block>		
	</xsl:template>
	
	<xsl:template match="eaxs:OrigDate">
		<fo:block white-space-collapse="false" linefeed-treatment="preserve" space-after="6pt" space-before="6pt">Date: <xsl:value-of select="msxsl:format-date(., 'dddd, MMM dd, yyyy, ')"/> <xsl:value-of select="msxsl:format-time(., ' h:m:s tt')"/></fo:block>		
	</xsl:template>

	<xsl:template match="eaxs:Subject">
		<fo:block white-space-collapse="false" linefeed-treatment="preserve" space-after="6pt" space-before="6pt">Subject: <xsl:apply-templates/></fo:block>		
	</xsl:template>

	<xsl:template match="eaxs:From">
		<fo:block white-space-collapse="false" linefeed-treatment="preserve" space-after="6pt" space-before="6pt">From: <xsl:apply-templates/></fo:block>		
	</xsl:template>
	
	<xsl:template match="eaxs:To">
		<fo:block white-space-collapse="false" linefeed-treatment="preserve" space-after="6pt" space-before="6pt">To: <xsl:apply-templates/></fo:block>		
	</xsl:template>

	<xsl:template match="eaxs:Cc">
		<fo:block white-space-collapse="false" linefeed-treatment="preserve" space-after="6pt" space-before="6pt">CC: <xsl:apply-templates/></fo:block>		
	</xsl:template>

	<xsl:template match="eaxs:Bcc">
		<fo:block white-space-collapse="false" linefeed-treatment="preserve" space-after="6pt" space-before="6pt">BCC: <xsl:apply-templates/></fo:block>		
	</xsl:template>

	<xsl:template match="eaxs:Content">
		<fo:block border="1.5pt solid black" padding="6pt" white-space-collapse="false" linefeed-treatment="preserve" space-after="6pt" space-before="6pt"><xsl:apply-templates/></fo:block>		
	</xsl:template>
	
</xsl:stylesheet>
