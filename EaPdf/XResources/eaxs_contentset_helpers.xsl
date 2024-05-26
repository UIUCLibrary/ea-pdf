<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0" 
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:fox="http://xmlgraphics.apache.org/fop/extensions"
    xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"
    xmlns:my="http://library.illinois.edu/myFunctions"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:fn="http://www.w3.org/2005/xpath-functions"
    >
    
    <!-- ========================================================================
		Some utility templates 
	============================================================================= -->
    
    <xsl:template name="InternalDestinationToMessageHeader">
        <xsl:param name="FolderOrMessage" select="."/>
        <xsl:choose>
            <xsl:when test="local-name($FolderOrMessage)='Folder'">
                <!-- link to the first message in the folder -->
                <xsl:attribute name="internal-destination">
                    <xsl:call-template name="ContentSetId">
                        <xsl:with-param name="type" select="'EmailHeaderRendering'"/>
                        <xsl:with-param name="number" select="($FolderOrMessage//eaxs:Message/eaxs:LocalId)[1]"/>							
                    </xsl:call-template>
                </xsl:attribute>                
            </xsl:when>
            <xsl:when test="local-name($FolderOrMessage)='Message'">
                <!-- link to this message -->
                <xsl:attribute name="internal-destination">
                    <xsl:call-template name="ContentSetId">
                        <xsl:with-param name="type" select="'EmailHeaderRendering'"/>
                        <xsl:with-param name="number" select="($FolderOrMessage/eaxs:LocalId)[1]"/>							
                    </xsl:call-template>
                </xsl:attribute>                                
            </xsl:when>
            <xsl:otherwise>
                <!-- link to the farthest away ancestor message, not to any child messages -->
                <xsl:attribute name="internal-destination">
                    <xsl:call-template name="ContentSetId">
                        <xsl:with-param name="type" select="'EmailHeaderRendering'"/>
                        <xsl:with-param name="number" select="$FolderOrMessage/ancestor::*[eaxs:MessageId][last()]/eaxs:LocalId"/>
                    </xsl:call-template>
                </xsl:attribute>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
    <xsl:template name="BeginContentSet">
        <xsl:param name="type" required="yes"/>
        <xsl:param name="subtype"/>
        <xsl:param name="number"/>
        <xsl:variable name="id">
            <xsl:call-template name="ContentSetId">
                <xsl:with-param name="type" select="$type"/>
                <xsl:with-param name="subtype" select="$subtype"/>
                <xsl:with-param name="number" select="$number"/>
            </xsl:call-template>
        </xsl:variable>
        <xsl:attribute name="page-break-before">always</xsl:attribute>
        <xsl:attribute name="id"><xsl:value-of select="$id"/></xsl:attribute>
        <fox:destination internal-destination="{$id}"/>
    </xsl:template>
    
    <xsl:template name="ContentSetId">
        <xsl:param name="type" required="yes"/>
        <xsl:param name="subtype"/>
        <xsl:param name="number"/>
        <xsl:value-of>
            <xsl:value-of select="concat('ContentSet_',$type)"/>
            <xsl:if test="$subtype">
                <!-- subtype is a MIME type so replace '/' with '_' to ensure valid names -->
                <xsl:value-of select="fn:lower-case(normalize-space(concat('_', replace($subtype,'/','_'))))"/>
            </xsl:if>
            <xsl:if test="$number">
                <xsl:value-of select="concat('_', $number)"/>
            </xsl:if>
        </xsl:value-of>        
    </xsl:template>
        
    
</xsl:stylesheet>
