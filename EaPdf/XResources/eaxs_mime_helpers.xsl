<?xml version="1.1" encoding="utf-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
    xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:fn="http://www.w3.org/2005/xpath-functions"
    xmlns:my="http://library.illinois.edu/myFunctions"
    >

    <!-- Reconstruct complete Content-Type header -->
    <xsl:template name="FullContentTypeHeader">
        <xsl:param name="Body" select=".."/>
        
        <xsl:value-of select="$Body/eaxs:ContentType"/>
        <xsl:if test="$Body/eaxs:Charset">; charset=<xsl:value-of select="my:QuoteIfNeeded($Body/eaxs:Charset)"/></xsl:if>
        <xsl:if test="$Body/eaxs:ContentName">; name=<xsl:value-of select="my:QuoteIfNeeded($Body/eaxs:ContentName)"/></xsl:if>
        <xsl:if test="$Body/eaxs:BoundaryString">; boundary=<xsl:value-of select="my:QuoteIfNeeded($Body/eaxs:BoundaryString)"/></xsl:if>
        <xsl:for-each select="$Body/eaxs:ContentTypeParam">
            <xsl:text>; </xsl:text><xsl:value-of select="eaxs:Name"/><xsl:text>=</xsl:text><xsl:value-of select="my:QuoteIfNeeded(eaxs:Value)"/>
        </xsl:for-each>
        <xsl:if test="$Body/eaxs:ContentTypeComments"> (<xsl:value-of select="replace($Body/eaxs:ContentTypeComments, '\(|\)|\\', '\\$0')"/>)</xsl:if>
    </xsl:template>
    
    <!-- If a string contains any special characters per email RFCs, it must be quoted -->
    <!-- TODO: Not sure this will handle non-ascii characters properly-->
    <xsl:function name="my:QuoteIfNeeded" as="xs:string">
        <xsl:param name="text" as="xs:string"/>
        <xsl:variable as="xs:string" name="tspecials" select="'\(|\)|&lt;|&gt;|@|,|;|:|\\|&quot;|/|\[|\]|\?|=| |[&#1;-&#31;]|&#127;'"/><!-- also SPACE and CTLs -->
        
        <xsl:choose>
            <xsl:when test="matches($text, $tspecials)">
                <xsl:value-of select="concat('&quot;',replace($text,'&quot;|\\', '\\$0'),'&quot;')"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="$text"/>
            </xsl:otherwise>
        </xsl:choose>
        
    </xsl:function>

</xsl:stylesheet>
