<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="2.0" 
	xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"

	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:xs="http://www.w3.org/2001/XMLSchema"
	xmlns:fn="http://www.w3.org/2005/xpath-functions"
	

	exclude-result-prefixes="eaxs xsl xs fn"
	>
	
	<!-- 
		This transformation creates a hierarchy of nodes that correspond to the DPart hierarchy that will be created in the EA-PDF document.
		The leaf nodes all correspond to EA-PDF Content Sets.
		
		DPart attributes prefixed with 'DPM_' will be turned into DPM metadata properties, for example "DPM_Mail_ContentSetType='FrontMatter'" will
		become "<</Type/DPart/.../DPM<</ContentSetType/FrontMatter>>/...>>" in the PDF.  The Id attribute ties the DPart node to a specific ContentSet in the PDF,
		allowing the DPart Start and End keys to be set.
	-->

	<xsl:include href="eaxs_contentset_helpers.xsl"/>
	
	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes" omit-xml-declaration="no" />
	
	<xsl:template match="/">
		<DPart>
			<DPart>
				<DPart DPM_Mail_ContentSetType="FrontMatter" DPM_Mail_Desc="This is the cover page for the archival email(s) in this document.">
					<xsl:attribute name="Id"><xsl:call-template name="ContentSetId"><xsl:with-param name="type" select="'FrontMatter'"/></xsl:call-template></xsl:attribute>				
				</DPart>
			</DPart>
			<xsl:apply-templates select="/eaxs:Account/eaxs:Folder[eaxs:Message or eaxs:Folder[.//eaxs:Message]]"/>
			<DPart>
				<DPart DPM_Mail_ContentSetType="AttachmentList" DPM_Mail_Desc="This is a list of all email attachments across all emails contained in this document.">
					<xsl:attribute name="Id"><xsl:call-template name="ContentSetId"><xsl:with-param name="type" select="'AttachmentList'"/></xsl:call-template></xsl:attribute>				
				</DPart>
			</DPart>
		</DPart>
	</xsl:template>

	<xsl:template match="eaxs:Folder">
		<DPart DPM_FolderName="{eaxs:Name}"><!-- This is a custom property not defined in the EA-PDF spec -->
			<xsl:apply-templates select="eaxs:Message"/>
			<xsl:apply-templates select="eaxs:Folder[eaxs:Message or eaxs:Folder[.//eaxs:Message]]"/>
		</DPart>
	</xsl:template>
	
	<xsl:template match="eaxs:Message">
		<DPart DPM_Mail_MessageID="{eaxs:MessageId}" DPM_Mail_GUID="{eaxs:Guid}">			
			<DPart DPM_Mail_ContentSetType="EmailHeaderRendering" DPM_Mail_Desc="This is a rendering of the important email headers for Message Id '{eaxs:MessageId}', including a listing of message contents.">
				<xsl:attribute name="Id">
					<xsl:call-template name="ContentSetId">
						<xsl:with-param name="type" select="'EmailHeaderRendering'"/>
						<xsl:with-param name="number" select="eaxs:LocalId"/>
					</xsl:call-template>
				</xsl:attribute>
			</DPart>
			
			<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody" />
			
		</DPart>
	</xsl:template>
	
	<xsl:template match="eaxs:MultiBody" >
		<xsl:choose>
			<xsl:when test="fn:lower-case(eaxs:ContentType) = 'multipart/alternative'">
				<xsl:for-each select="eaxs:SingleBody | eaxs:MultiBody"><!-- TODO: MissingBody -->
					<xsl:sort select="position()" data-type="number" order="descending"/> <!-- alternatives have priority in descending order, so the last is displayed first -->
					<xsl:apply-templates select="." />
				</xsl:for-each>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="eaxs:SingleBody | eaxs:MultiBody" />				
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	<xsl:template match="eaxs:SingleBody" >
		<!-- only render child messages if they contain plain text or html content -->
		<xsl:apply-templates select="eaxs:BodyContent | eaxs:ExtBodyContent | eaxs:DeliveryStatus"/>
	</xsl:template>
	
	<xsl:template match="eaxs:BodyContent">
		<xsl:variable name="ContentType" select="fn:lower-case(normalize-space(../eaxs:ContentType))"/>
		<xsl:variable name="ContentTypeHuman">
			<xsl:call-template name="HumanReadableContentType"/>
		</xsl:variable>
		<!-- only render content which is text and which is not an attachment -->
		<xsl:if test="not(fn:lower-case(normalize-space(../@IsAttachment)) = 'true') and starts-with($ContentType,'text/')">
			<DPart DPM_Mail_ContentSetType="BodyRendering" DPM_Mail_Subtype="{fn:lower-case(normalize-space(parent::eaxs:SingleBody/eaxs:ContentType))}" DPM_Mail_Desc="This is a rendering of the {$ContentTypeHuman} body for Message Id '{ancestor::eaxs:Message/eaxs:MessageId}'.">
				<xsl:attribute name="Id">
					<xsl:call-template name="ContentSetId">
						<xsl:with-param name="type">BodyRendering</xsl:with-param>
						<xsl:with-param name="subtype"><xsl:value-of select="parent::eaxs:SingleBody/eaxs:ContentType"/></xsl:with-param>
						<!--There can be multiple bodies with the same MIME type which could result in duplicate ids, so need to add a counter to the number -->
						<xsl:with-param name="number">
							<xsl:value-of select="ancestor::eaxs:Message/eaxs:LocalId"/>
							<xsl:text>.</xsl:text>
							<xsl:value-of select="count(ancestor::*/preceding-sibling::*[fn:lower-case(normalize-space(eaxs:ContentType)) = $ContentType])"/>
						</xsl:with-param> 
					</xsl:call-template>
				</xsl:attribute>
			</DPart>
		</xsl:if>
	</xsl:template>
	
	<xsl:template match="eaxs:ExtBodyContent">
		<!-- TODO -->
	</xsl:template>
	
	<xsl:template match="eaxs:DeliveryStatus">
		<xsl:variable name="ContentType" select="fn:lower-case(normalize-space(../eaxs:ContentType))"/>
		<xsl:variable name="ContentTypeHuman">
			<xsl:call-template name="HumanReadableContentType"/>
		</xsl:variable>
		<DPart DPM_Mail_ContentSetType="BodyRendering" DPM_Mail_Subtype="{fn:lower-case(normalize-space(parent::eaxs:SingleBody/eaxs:ContentType))}" DPM_Mail_Desc="This is a rendering of the {$ContentTypeHuman} body for Message Id '{ancestor::eaxs:Message/eaxs:MessageId}'.">
			<xsl:attribute name="Id">
				<xsl:call-template name="ContentSetId">
					<xsl:with-param name="type">BodyRendering</xsl:with-param>
					<xsl:with-param name="subtype"><xsl:value-of select="parent::eaxs:SingleBody/eaxs:ContentType"/></xsl:with-param>
					<!-- There can be multiple bodies with the same MIME type which could result in duplicate ids -->
					<xsl:with-param name="number">
						<xsl:value-of select="ancestor::eaxs:Message/eaxs:LocalId"/>
						<xsl:text>.</xsl:text>
						<xsl:value-of select="count(ancestor::*/preceding-sibling::*[fn:lower-case(normalize-space(eaxs:ContentType)) = $ContentType])"/>
					</xsl:with-param> 
				</xsl:call-template>
			</xsl:attribute>
		</DPart>
	</xsl:template>

	<xsl:template name="HumanReadableContentType">
		<xsl:param name="ContentType" select="parent::eaxs:SingleBody/eaxs:ContentType"/>
		<xsl:variable name="ContentTypeNorm" select="fn:lower-case(normalize-space($ContentType))"/>
		<xsl:choose>
			<xsl:when test="$ContentTypeNorm = 'text/plain'">plain text</xsl:when>
			<xsl:when test="$ContentTypeNorm = 'text/html'">HTML</xsl:when>
			<xsl:when test="$ContentTypeNorm = 'application/xhtml+xml'">XHTML</xsl:when>
			<xsl:when test="$ContentTypeNorm = 'message/delivery-status'">delivery status</xsl:when>
			<xsl:otherwise>
				'<xsl:value-of select="$ContentTypeNorm"/>'
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>



</xsl:stylesheet>
