<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
    xmlns:ht="http://www.w3.org/1999/xhtml"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"
    >
    <!-- Author: Alberto González Palomo http://sentido-labs.com
       2016-12-24 00:20 -->
    
    <!-- BEGIN checks -->
    
    <xsl:template match="//ht:tbody | //ht:table[not(ht:tbody)]">
        <xsl:variable name="row-count" select="count(ht:tr)"/>
        
        <xsl:for-each select="ht:tr/ht:td[@rowspan] | ht:tr/ht:th[@rowspan]">
            <xsl:variable name="last-row-spanned" select="count(parent::ht:tr/preceding-sibling::ht:tr) + @rowspan"/>
            <xsl:if test="$last-row-spanned &gt; $row-count">
                <xsl:call-template name="error">
                    <xsl:with-param name="message">A table-cell is spanning more rows than available in its parent element.</xsl:with-param>
                </xsl:call-template>
            </xsl:if>
        </xsl:for-each>
    </xsl:template>
    
    <!-- END checks -->
    
    
    <xsl:output method="text"/>
    
    <xsl:template match="text()"><!-- Omit text content. --></xsl:template>
    
    <xsl:template name="error">
        <xsl:param name="location" select="."/>
        <xsl:param name="message">No error message available.</xsl:param>
        
        <!-- To stop at the first error, set the attribute terminate="yes".
         In xsltproc, this also causes the process to return a failure value.
         -->
        <xsl:message terminate="no">
            <xsl:text>Error: </xsl:text>
            <xsl:text> [Folder: </xsl:text><xsl:value-of select="ancestor::eaxs:Folder/eaxs:FolderProperties/eaxs:RelPath"/><xsl:text> Message Id: </xsl:text><xsl:value-of select="ancestor::eaxs:Message/eaxs:MessageId"/><xsl:text>] </xsl:text>
            <xsl:call-template name="xpath">
                <xsl:with-param name="location" select="$location"/>
            </xsl:call-template>
            <xsl:text>: </xsl:text>
            <xsl:value-of select="$message"/>
        </xsl:message>
    </xsl:template>
    
    <xsl:template name="xpath">
        <xsl:param name="location" select="."/>
        <xsl:for-each select="$location/parent::*">
            <xsl:call-template name="xpath"/>
        </xsl:for-each>
        
        <xsl:text>/</xsl:text>
        <xsl:variable name="element-name" select="name($location)"/>
        <xsl:value-of select="$element-name"/>
        
        <xsl:variable name="preceding" select="count($location/preceding-sibling::*[name() = $element-name])"/>
        <xsl:variable name="following" select="count($location/following-sibling::*[name() = $element-name])"/>
        <xsl:if test="$preceding + $following &gt; 0">
            <xsl:text>[</xsl:text>
            <xsl:value-of select="1 + $preceding"/>
            <xsl:text>]</xsl:text>
        </xsl:if>
    </xsl:template>
    
</xsl:stylesheet>
