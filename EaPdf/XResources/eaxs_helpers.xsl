<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE stylesheet SYSTEM "eaxs_entities.ent">

<xsl:stylesheet version="2.0" 
    xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"
    
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:fo="http://www.w3.org/1999/XSL/Format"
    xmlns:html="http://www.w3.org/1999/xhtml"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:fn="http://www.w3.org/2005/xpath-functions"
    xmlns:my="http://library.illinois.edu/myFunctions"
    
    xmlns:pdf="http://xmlgraphics.apache.org/fop/extensions/pdf"
    xmlns:fox="http://xmlgraphics.apache.org/fop/extensions"
    
    xmlns:rx="http://www.renderx.com/XSL/Extensions"
    >
    
    <xsl:variable name="AbsoluteNumeric">^\s*(\d+(?:\.\d*)*|\.\d+)(cm|mm|in|pt|pc|px)?\s*$</xsl:variable>
    
    <xsl:output method="xml" version="1.0" encoding="utf-8" indent="no" omit-xml-declaration="no" cdata-section-elements="rx:custom-meta"/>
    
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
                <xsl:value-of select="concat('_', $subtype)"/>
            </xsl:if>
            <xsl:if test="$number">
                <xsl:value-of select="concat('_', $number)"/>
            </xsl:if>
        </xsl:value-of>        
    </xsl:template>
    
    <xsl:template name="GetImgAltAttr">
        <xsl:param name="inline"/>
        <xsl:param name="preferred"/>
        <xsl:param name="default"/>
        <xsl:variable name="value">
            <xsl:call-template name="GetImgAltText">
                <xsl:with-param name="inline" select="$inline"/>
                <xsl:with-param name="preferred" select="$preferred"/>
                <xsl:with-param name="default" select="$default"/>                
            </xsl:call-template>            
        </xsl:variable>
        <xsl:choose>
            <xsl:when test="fn:lower-case(fn:normalize-space($fo-processor))='fop'">
                <xsl:attribute name="fox:alt-text" select="$value"/>                
            </xsl:when>
            <xsl:when test="fn:lower-case(fn:normalize-space($fo-processor))='xep'">
                <xsl:attribute name="rx:alt-description" select="$value"/>                
            </xsl:when>
            <xsl:otherwise>
                <xsl:message terminate="yes">Unexpected FO processor: <xsl:value-of select="$fo-processor-version"/></xsl:message>
            </xsl:otherwise>
        </xsl:choose>
     </xsl:template>
    
    <xsl:template name="GetImgAltText">
        <xsl:param name="inline"/>
        <xsl:param name="preferred"/>
        <xsl:param name="default"/>
        <xsl:choose>
            <xsl:when test="$preferred">
                <xsl:value-of select="$preferred"/>													
            </xsl:when>													
            <xsl:when test="$inline/eaxs:Description">
                <xsl:value-of select="$inline/eaxs:Description"/>													
            </xsl:when>													
            <xsl:when test="$inline/eaxs:ContentName">
                <xsl:value-of select="$inline/eaxs:ContentName"/>													
            </xsl:when>													
            <xsl:when test="$inline/eaxs:DispositionFileName">
                <xsl:value-of select="$inline/eaxs:DispositionFileName"/>													
            </xsl:when>
            <xsl:when test="$default">
                <xsl:value-of select="$default"/>													
            </xsl:when>
            <xsl:otherwise>
                <xsl:text>Attached Image; Alternate description is unavailable</xsl:text>
                <xsl:message>Alternate text for image is not available</xsl:message>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
    <xsl:template name="GetURLForAttachedContent">
        <xsl:param name="inline"/>
        <xsl:choose>
            <xsl:when test="$inline/eaxs:ExtBodyContent">
                <xsl:variable name="rel-path">
                    <xsl:call-template name="concat-path">
                        <xsl:with-param name="path1" select="normalize-space($inline/ancestor::eaxs:Message/eaxs:RelPath)"/>
                        <xsl:with-param name="path2" select="normalize-space($inline/eaxs:ExtBodyContent/eaxs:RelPath)"/>
                    </xsl:call-template>
                </xsl:variable>
                <xsl:choose>
                    <xsl:when test="fn:normalize-space(fn:lower-case($inline/eaxs:ExtBodyContent/eaxs:XMLWrapped)) = 'false'">
                        <xsl:text>url('</xsl:text><xsl:value-of select="fn:resolve-uri($rel-path, fn:base-uri())"/><xsl:text>')</xsl:text>
                    </xsl:when>
                    <xsl:when test="fn:normalize-space(fn:lower-case($inline/eaxs:ExtBodyContent/eaxs:XMLWrapped)) = 'true'">
                        <xsl:call-template name="data-uri">
                            <xsl:with-param name="transfer-encoding" select="document(fn:resolve-uri($rel-path, fn:base-uri()))/eaxs:BodyContent/eaxs:TransferEncoding"/>
                            <xsl:with-param name="content-type" select="$inline/eaxs:ContentType"/>
                            <xsl:with-param name="content" select="document(fn:resolve-uri($rel-path, fn:base-uri()))/eaxs:BodyContent/eaxs:Content"/>
                        </xsl:call-template>
                    </xsl:when>
                </xsl:choose>
            </xsl:when>
            <xsl:when test="$inline/eaxs:BodyContent">
                <xsl:call-template name="data-uri">
                    <xsl:with-param name="content-type" select="$inline/eaxs:ContentType"/>
                    <xsl:with-param name="transfer-encoding" select="$inline/eaxs:BodyContent/eaxs:TransferEncoding"/>
                    <xsl:with-param name="content" select="$inline/eaxs:BodyContent/eaxs:Content"/>
                </xsl:call-template>
            </xsl:when>
            <xsl:otherwise>
                <xsl:message terminate="yes">Unexpected SingleBody Content</xsl:message>
            </xsl:otherwise>
        </xsl:choose>
        
    </xsl:template>
    
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
            <xsl:when test="fn:lower-case(normalize-space($single-body/eaxs:ContentType)) = 'application/pdf'">.pdf</xsl:when>
            <xsl:when test="fn:lower-case(normalize-space($single-body/eaxs:ContentType)) = 'text/rtf'">.rtf</xsl:when>
            
            <!-- use the extension from the content name or disposition filename if there is one -->
            <xsl:when test="fn:contains($single-body/eaxs:DispositionFileName,'.')">
                <xsl:text>.</xsl:text><xsl:value-of select="fn:tokenize($single-body/eaxs:DispositionFileName,'\.')[last()]"/>
            </xsl:when>
            <xsl:when test="fn:contains($single-body/eaxs:ContentName,'.')">
                <xsl:text>.</xsl:text><xsl:value-of select="fn:tokenize($single-body/eaxs:ContentName,'\.')[last()]"/>
            </xsl:when>
            
            <!-- fallback to just 'bin' -->
            <xsl:otherwise>.bin</xsl:otherwise>
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
            <xsl:variable name="UniqueDestination">
                <xsl:text>X_</xsl:text><xsl:value-of select="$single-body/eaxs:*/eaxs:Hash/eaxs:Value"/><xsl:text>_</xsl:text><xsl:value-of select="$single-body/ancestor::*/eaxs:LocalId"/>
            </xsl:variable>
            <xsl:choose>
                <xsl:when test="$fo-processor='fop'">
                    <fo:inline>&nbsp;(</fo:inline>
                    <fo:inline font-size="small">
                        <xsl:attribute name="id">M<xsl:value-of select="$UniqueDestination"/></xsl:attribute>
                        <fox:destination><xsl:attribute name="internal-destination">M<xsl:value-of select="$UniqueDestination"/></xsl:attribute></fox:destination>
                        <xsl:text>Attachment</xsl:text>
                    </fo:inline>	
                    <fo:inline>&nbsp;</fo:inline>
                    <fo:basic-link>
                        <xsl:attribute name="id"><xsl:value-of select="$UniqueDestination"/></xsl:attribute>
                        <xsl:attribute name="internal-destination"><xsl:value-of select="$UniqueDestination"/></xsl:attribute>
                        <fox:destination><xsl:attribute name="internal-destination"><xsl:value-of select="$UniqueDestination"/></xsl:attribute></fox:destination>
                        <fo:inline xsl:use-attribute-sets="a-link" font-size="small">&nbsp; </fo:inline>	
                    </fo:basic-link>
                    <fo:inline>&nbsp;)</fo:inline>
                </xsl:when>
                <xsl:when test="$fo-processor='xep'">
                    <fo:inline>&nbsp;(</fo:inline>
                    <fo:inline font-size="small">
                        <xsl:attribute name="id">M<xsl:value-of select="$UniqueDestination"/></xsl:attribute>
                        <fox:destination><xsl:attribute name="internal-destination">M<xsl:value-of select="$UniqueDestination"/></xsl:attribute></fox:destination>
                        <xsl:text>Attachment</xsl:text>
                    </fo:inline>	
                    <fo:inline font-size="small">
                        <xsl:text>&nbsp;</xsl:text>
                        <rx:pdf-comment>
                            <xsl:attribute name="title">Attachment &mdash; </xsl:attribute>
                            <rx:pdf-file-attachment icon-type="paperclip">
                                <xsl:attribute name="filename"><xsl:value-of select="$single-body/eaxs:*/eaxs:Hash/eaxs:Value"/><xsl:value-of select="$file-ext"/></xsl:attribute>
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
                    <fo:inline>&nbsp; )</fo:inline>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:message terminate="yes">
                        <xsl:text>Error: (name='AttachmentLink') The value '</xsl:text><xsl:value-of select="$fo-processor-version"/><xsl:text>' is not a valid value for fo-processor-version param; allowed values must start with 'FOP' or 'XEP'.</xsl:text>
                    </xsl:message>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:if>
    </xsl:template>
    


    <!-- Functions for filepath manipulation, inspired by https://stackoverflow.com/questions/3116942/doing-file-path-manipulations-in-xslt-->
    <!-- Tried to accomodate both Windows and Linux and URL path separators, probably not as robust as it could be -->
    
    <!-- Escape a string according to PDF Spec 32000 7.3.5 -->
    <xsl:function name="my:PdfNameEscape" as="xs:string">
        <xsl:param name="instring" as="xs:string"/>
        <!-- URI encode the string and then replace the % with a #; %HH become #HH -->
        <xsl:value-of select="fn:replace(fn:encode-for-uri($instring),'%','#')"/>
    </xsl:function>
    
    <!-- Return a width and height that do not exceed the width of the PDF page, accounting for margins -->
    <!-- TODO: Need check that the units are absolute numerics -->
    <xsl:function name="my:ScaleToBodyWidth" as="xs:string">
        <xsl:param name="value" as="xs:string"/>
        
        <xsl:choose>
            <xsl:when test="fn:matches($value,$AbsoluteNumeric,'i')">
                
                <xsl:variable name="body-width-pts" select="my:PageBodyWidthPts()"/>
                
                <xsl:variable name="value-pts" select="my:ScaleLength(my:ConvertToPoints($value), $body-width-pts)"/>
                
                <xsl:if test="my:ConvertToPoints($value) > $body-width-pts">
                    <xsl:message>Width '<xsl:value-of select="$value"/>' exceeded the page body width and was scaled down to '<xsl:value-of select="fn:concat($value-pts div 72,'in')"/>'.</xsl:message>
                </xsl:if>

                <xsl:value-of select="fn:concat($value-pts div 72,'in')"/>
                
            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="$value"/>
            </xsl:otherwise>
        </xsl:choose>
        
        
    </xsl:function>
    
    <xsl:function name="my:ScaleToBodyHeight" as="xs:string">
        <xsl:param name="value" as="xs:string"/>
        
        <xsl:choose>
            <xsl:when test="fn:matches($value,$AbsoluteNumeric,'i')">
                <xsl:variable name="body-height-pts" select="my:PageBodyHeightPts()"/>
                
                <xsl:variable name="value-pts" select="my:ScaleLength(my:ConvertToPoints($value), $body-height-pts)"/>
                
                <xsl:if test="my:ConvertToPoints($value) > $body-height-pts">
                    <xsl:message>Height '<xsl:value-of select="$value"/>' exceeded the page body height and was scaled down to '<xsl:value-of select="fn:concat($value-pts div 72,'in')"/>'.</xsl:message>
                </xsl:if>
                
                <xsl:value-of select="fn:concat($value-pts div 72,'in')"/>

            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="$value"/>
            </xsl:otherwise>
        </xsl:choose>
        
    </xsl:function>

    <xsl:function name="my:PageBodyWidthPts" as="xs:float">
        <xsl:variable name="page-width-pts" select="my:ConvertToPoints($page-width)"/>
        <xsl:variable name="page-margin-left-pts" select="my:ConvertToPoints($page-margin-left)"/>
        <xsl:variable name="page-margin-right-pts" select="my:ConvertToPoints($page-margin-right)"/>
        
        <xsl:value-of select="$page-width-pts - $page-margin-left-pts - $page-margin-right-pts - 48"/>      
    </xsl:function>
    
    <xsl:function name="my:PageBodyHeightPts" as="xs:float">
        <xsl:variable name="page-height-pts" select="my:ConvertToPoints($page-height)"/>
        <xsl:variable name="page-margin-top-pts" select="my:ConvertToPoints($page-margin-top)"/>
        <xsl:variable name="page-margin-bottom-pts" select="my:ConvertToPoints($page-margin-bottom)"/>
        
        <xsl:value-of select="$page-height-pts - $page-margin-top-pts - $page-margin-bottom-pts - 18"/>
        
    </xsl:function>
    
    <xsl:function name="my:ScaleLength" as="xs:float">
        <xsl:param name="value" as="xs:float"/>
        <xsl:param name="max" as="xs:float"/>
        
        <xsl:choose>
            <xsl:when test="$value > $max">
                <xsl:value-of select="$max"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="$value"/>
            </xsl:otherwise>
        </xsl:choose>
        
    </xsl:function>
    
    <xsl:function name="my:StyleContainsProperty" as="xs:string">
        <xsl:param name="style" as="xs:string"/>
        <xsl:param name="property" as="xs:string"/>
        
        <xsl:variable name="regex" select="fn:concat('(?:\s|^\s*|;\s*)',fn:normalize-space($property),'\s*:\s*([^;]+)')"/>
        
        <xsl:variable name="matches" select="fn:analyze-string($style,$regex,'i')"/>
        
        <xsl:value-of select="fn:normalize-space($matches/fn:match[1]/fn:group[1])"/>
               
    </xsl:function>
    
    <xsl:function name="my:ConvertToPoints" as="xs:float">
        <xsl:param name="value" as="xs:string"/>

        <xsl:variable name="matches" select="fn:analyze-string($value,$AbsoluteNumeric,'i')"/>
        <xsl:variable name="number" select="$matches/fn:match[1]/fn:group[1]"/>
        <xsl:variable name="unit" select="$matches/fn:match[1]/fn:group[2]"/>
        
        <xsl:choose>
            <xsl:when test="fn:lower-case(fn:normalize-space($unit))='cm'">
                <xsl:value-of select="$number div 2.54 * 72"/>
            </xsl:when>
            <xsl:when test="fn:lower-case(fn:normalize-space($unit))='mm'">
                <xsl:value-of select="$number div 25.4 * 72"/>                
            </xsl:when>
            <xsl:when test="fn:lower-case(fn:normalize-space($unit))='in'">
                <xsl:value-of select="$number * 72"/>                                
            </xsl:when>
            <xsl:when test="fn:lower-case(fn:normalize-space($unit))='pt' or ($number and not($unit))">
                <xsl:value-of select="$number"/>
            </xsl:when>
            <xsl:when test="fn:lower-case(fn:normalize-space($unit))='pc'">
                <xsl:value-of select="$number * 12"/>
            </xsl:when>
            <xsl:when test="fn:lower-case(fn:normalize-space($unit))='px'">
                <xsl:value-of select="$number div $dpi * 72"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:message terminate="yes">Unexpected unit: <xsl:value-of select="$value"/></xsl:message>
            </xsl:otherwise>
        </xsl:choose>
        
    </xsl:function>
    
    <!-- Given a full path, return just the file name or last path segment component -->
    <xsl:function name="my:GetFileName" as="xs:string">
        <xsl:param name="pfile" as="xs:string"/>
        <xsl:variable name="separator" select="my:GetPathSepartor($pfile)"/>
        <xsl:sequence select="my:ReverseString(substring-before(my:ReverseString($pfile), $separator))" />								
    </xsl:function>
    
    <!-- Given a full path, return just the directory name or complete path minus the last segment, excluding path separator -->
    <xsl:function name="my:GetDirectoryName" as="xs:string">
        <xsl:param name="pfile" as="xs:string"/>
        <xsl:variable name="separator" select="my:GetPathSepartor($pfile)"/>
        <xsl:sequence select="my:ReverseString(substring-after(my:ReverseString($pfile), $separator))" />
    </xsl:function>
    
    <!-- combine two path segments using the appropriate path separator, can accomodate segments having trailing or leading separators without duplicating separators -->
    <xsl:function name="my:Combine" as="xs:string">
        <xsl:param name="p1" as="xs:string"/>
        <xsl:param name="p2" as="xs:string"/>
        <xsl:variable name="separator" select="my:GetPathSepartor(fn:concat($p1,$p2))"/>
        <xsl:variable name="p1x">
            <xsl:choose>
                <xsl:when test="fn:substring($p1, fn:string-length($p1), 1) = $separator"><xsl:value-of select="fn:substring($p1,1,fn:string-length($p1) - 1)"/></xsl:when>
                <xsl:otherwise><xsl:value-of select="$p1"/></xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <xsl:variable name="p2x">
            <xsl:choose>
                <xsl:when test="fn:substring($p2,1,1) = $separator"><xsl:value-of select="fn:substring($p2,2)"/></xsl:when>
                <xsl:otherwise><xsl:value-of select="$p2"/></xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <xsl:value-of select="fn:concat($p1x,$separator, $p2x)"/>
    </xsl:function>
    
    <xsl:function name="my:ReverseString" as="xs:string">
        <xsl:param name="pStr" as="xs:string"/>
        <xsl:sequence select="codepoints-to-string(reverse(string-to-codepoints($pStr)))"/>
    </xsl:function>
    
    <!-- looks at a path string and determines the appropriate separator, defaults to '/', assumes different separators will not be combined in the same path -->
    <xsl:function name="my:GetPathSepartor" as="xs:string">
        <xsl:param name="pfile" as="xs:string"/>
        <xsl:choose>
            <xsl:when test="fn:contains($pfile,'\')">\</xsl:when>
            <xsl:otherwise>/</xsl:otherwise>
        </xsl:choose>		
    </xsl:function>
    
    <!-- Resolves a relative file path against the base file path, similar to the resolve-uri function, except non-western unicode characters are not URL Encoded -->
    <xsl:function name="my:GetPathRelativeToBaseUri" as="xs:string">
        <xsl:param name="pfile" as="xs:string" />
        <xsl:param name="baseUri" as="xs:string"/>
        <xsl:value-of select="my:Combine(my:GetDirectoryName($baseUri), $pfile)"/>
    </xsl:function>
    
</xsl:stylesheet>
