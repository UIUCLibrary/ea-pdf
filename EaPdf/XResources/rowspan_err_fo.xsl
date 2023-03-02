<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
    xmlns:fo="http://www.w3.org/1999/XSL/Format"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:saxon="http://saxon.sf.net/"
    >
    <!-- Based on https://stackoverflow.com/questions/41123392/detecting-table-element-error-in-docbook-5-0-document -->
    <!-- Author: Alberto González Palomo http://sentido-labs.com
       2016-12-24 00:20 -->
    
    <!-- BEGIN checks -->
    
    <xsl:template match="//fo:table-body">
        <xsl:variable name="row-count" select="count(fo:table-row)"/>
        
        <xsl:for-each select="fo:table-row/fo:table-cell[@number-rows-spanned]">
            <xsl:variable name="last-row-spanned" select="count(parent::fo:table-row/preceding-sibling::fo:table-row) + @number-rows-spanned"/>
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
         <xsl:message terminate="no">
             <xsl:text>Error: [</xsl:text><xsl:value-of select="saxon:line-number()"/><xsl:text>]</xsl:text>
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
