<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE stylesheet SYSTEM "eaxs_entities.ent">

<!--

Copyright Antenna House, Inc. (http://www.antennahouse.com) 2001, 2002.

Since this stylesheet is originally developed by Antenna House to be used with XSL Formatter, it may not be compatible with another XSL-FO processors.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to 
deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, and/or sell copies 
of the Software, and to permit persons to whom the Software is furnished to do so, provided that the above copyright notice(s) and this permission 
notice appear in all copies of the Software and that both the above copyright notice(s) and this permission notice appear in supporting documentation.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF THIRD PARTY RIGHTS. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN THIS NOTICE 
BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES, OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, 
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

-->

<!-- Modified by Tom Habing, 2023-01-12 for EMAIL to PDF conversion project -->

<xsl:stylesheet version="2.0"
                xmlns:html="http://www.w3.org/1999/xhtml"
                xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"
                
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:fo="http://www.w3.org/1999/XSL/Format"
                xmlns:fn="http://www.w3.org/2005/xpath-functions"
                xmlns:my="http://library.illinois.edu/myFunctions"
                
                xmlns:pdf="http://xmlgraphics.apache.org/fop/extensions/pdf"
                xmlns:fox="http://xmlgraphics.apache.org/fop/extensions"
                
                xmlns:rx="http://www.renderx.com/XSL/Extensions"
                  >

  <xsl:key name="HTML_IDS" match="//html:*[@id]" use="@id"  />
  <xsl:key name="HTML_NAMES" match="//html:a[@name]" use="@name"  />
  
  <!--======================================================================
      Parameters
  =======================================================================-->


  <!-- page header and footer -->
  <xsl:param name="page-header-margin">0.5in</xsl:param>
  <xsl:param name="page-footer-margin">0.5in</xsl:param>
  <xsl:param name="title-print-in-header">true</xsl:param>
  <xsl:param name="page-number-print-in-footer">true</xsl:param>

  <!-- multi column -->
  <xsl:param name="column-count">1</xsl:param>
  <xsl:param name="column-gap">12pt</xsl:param>

  <!-- writing-mode: lr-tb | rl-tb | tb-rl -->
  <xsl:param name="writing-mode">lr-tb</xsl:param>

  <!-- text-align: justify | start -->
  <xsl:param name="text-align">start</xsl:param>

  <!-- hyphenate: true | false -->
  <xsl:param name="hyphenate">false</xsl:param>
  

  <!-- TGH For increased performance and also security set this to false -->
  <xsl:param name="load-external-images">false</xsl:param>
  
  
  <!-- TGH:  Apache FOP does not support font-weight bolder or lighter, so use bold or normal instead -->
  <xsl:variable name="Bolder">
    <xsl:choose>
      <xsl:when test="$fo-processor = 'fop'">bold</xsl:when>
      <xsl:otherwise>bolder</xsl:otherwise>
    </xsl:choose>
  </xsl:variable>
  <xsl:variable name="Lighter">
    <xsl:choose>
      <xsl:when test="$fo-processor = 'fop'">normal</xsl:when>
      <xsl:otherwise>lighter</xsl:otherwise>
    </xsl:choose>
  </xsl:variable>
  
  
  <!--======================================================================
      Attribute Sets
  =======================================================================-->

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Root
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:attribute-set name="root">
    <xsl:attribute name="writing-mode"><xsl:value-of select="$writing-mode"/></xsl:attribute>
    <xsl:attribute name="hyphenate"><xsl:value-of select="$hyphenate"/></xsl:attribute>
    <xsl:attribute name="text-align"><xsl:value-of select="$text-align"/></xsl:attribute>
    <!-- specified on fo:root to change the properties' initial values -->
  </xsl:attribute-set>

  <xsl:attribute-set name="page">
    <xsl:attribute name="page-width"><xsl:value-of select="$page-width"/></xsl:attribute>
    <xsl:attribute name="page-height"><xsl:value-of select="$page-height"/></xsl:attribute>
    <!-- specified on fo:simple-page-master -->
  </xsl:attribute-set>

  <xsl:attribute-set name="body">
    <!-- specified on fo:flow's only child fo:block -->
  </xsl:attribute-set>

  <xsl:attribute-set name="page-header">
    <!-- specified on (page-header)fo:static-content's only child fo:block -->
    <xsl:attribute name="font-size">small</xsl:attribute>
    <xsl:attribute name="text-align">center</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="page-footer">
    <!-- specified on (page-footer)fo:static-content's only child fo:block -->
    <xsl:attribute name="font-size">small</xsl:attribute>
    <xsl:attribute name="text-align">center</xsl:attribute>
  </xsl:attribute-set>
  
  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Block-level
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:attribute-set name="h1" use-attribute-sets="h1-font h1-space">
    <xsl:attribute name="keep-with-next.within-column">always</xsl:attribute>
    <xsl:attribute name="keep-together.within-column">always</xsl:attribute>
  </xsl:attribute-set>
  
  <xsl:attribute-set name="h1-font">
    <xsl:attribute name="font-size">2em</xsl:attribute>
    <xsl:attribute name="font-weight">bold</xsl:attribute>
   </xsl:attribute-set>

  <xsl:attribute-set name="h1-space" >
    <xsl:attribute name="space-before">0.67em</xsl:attribute>
    <xsl:attribute name="space-after">0.67em</xsl:attribute>
  </xsl:attribute-set>
  
  
  <xsl:attribute-set name="h2" use-attribute-sets="h2-font h2-space">
    <xsl:attribute name="keep-with-next.within-column">always</xsl:attribute>
    <xsl:attribute name="keep-together.within-column">always</xsl:attribute>
  </xsl:attribute-set>
  
  <xsl:attribute-set name="h2-font">
    <xsl:attribute name="font-size">1.5em</xsl:attribute>
    <xsl:attribute name="font-weight">bold</xsl:attribute>
  </xsl:attribute-set>
  
  <xsl:attribute-set name="h2-space" >
    <xsl:attribute name="space-before">0.83em</xsl:attribute>
    <xsl:attribute name="space-after">0.83em</xsl:attribute>
  </xsl:attribute-set>
  
  <xsl:attribute-set name="h3" use-attribute-sets="h3-font h3-space">
    <xsl:attribute name="keep-with-next.within-column">always</xsl:attribute>
    <xsl:attribute name="keep-together.within-column">always</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="h3-font">
    <xsl:attribute name="font-size">1.17em</xsl:attribute>
    <xsl:attribute name="font-weight">bold</xsl:attribute>
  </xsl:attribute-set>
  
  <xsl:attribute-set name="h3-space" >
    <xsl:attribute name="space-before">1em</xsl:attribute>
    <xsl:attribute name="space-after">1em</xsl:attribute>
  </xsl:attribute-set>
  
  <xsl:attribute-set name="h4" use-attribute-sets="h4-font h4-space">
    <xsl:attribute name="keep-with-next.within-column">always</xsl:attribute>
    <xsl:attribute name="keep-together.within-column">always</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="h4-font">
    <xsl:attribute name="font-size">1em</xsl:attribute>
    <xsl:attribute name="font-weight">bold</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="h4-space">
    <xsl:attribute name="space-before">1.17em</xsl:attribute>
    <xsl:attribute name="space-after">1.17em</xsl:attribute>
  </xsl:attribute-set>
  
  <xsl:attribute-set name="h5" use-attribute-sets="h5-font h5-space">
    <xsl:attribute name="keep-with-next.within-column">always</xsl:attribute>
    <xsl:attribute name="keep-together.within-column">always</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="h5-font">
    <xsl:attribute name="font-size">0.83em</xsl:attribute>
    <xsl:attribute name="font-weight">bold</xsl:attribute>
   </xsl:attribute-set>
  
  <xsl:attribute-set name="h5-space">
    <xsl:attribute name="space-before">1.33em</xsl:attribute>
    <xsl:attribute name="space-after">1.33em</xsl:attribute>
  </xsl:attribute-set>
  
  <xsl:attribute-set name="h6" use-attribute-sets="h6-font h6-space">
    <xsl:attribute name="keep-with-next.within-column">always</xsl:attribute>
    <xsl:attribute name="keep-together.within-column">always</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="h6-font">
    <xsl:attribute name="font-size">0.67em</xsl:attribute>
    <xsl:attribute name="font-weight">bold</xsl:attribute>
  </xsl:attribute-set>
  
  <xsl:attribute-set name="h6-space">
    <xsl:attribute name="space-before">1.67em</xsl:attribute>
    <xsl:attribute name="space-after">1.67em</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="p">
    <xsl:attribute name="space-before">0</xsl:attribute>
    <xsl:attribute name="space-after">0.125em</xsl:attribute>
    <!-- e.g.,
    <xsl:attribute name="text-indent">1em</xsl:attribute>
    -->
  </xsl:attribute-set>

  <xsl:attribute-set name="p-initial" use-attribute-sets="p">
    <!-- initial paragraph, preceded by h1..6 or div -->
    <!-- e.g.,
    <xsl:attribute name="text-indent">0em</xsl:attribute>
    -->
  </xsl:attribute-set>

  <xsl:attribute-set name="p-initial-first" use-attribute-sets="p-initial">
    <!-- initial paragraph, first child of div, body or td -->
  </xsl:attribute-set>

  <xsl:attribute-set name="blockquote">
    <xsl:attribute name="start-indent">inherited-property-value(start-indent) + 24pt</xsl:attribute>
    <xsl:attribute name="end-indent">inherited-property-value(end-indent) + 24pt</xsl:attribute>
    <xsl:attribute name="space-before">1em</xsl:attribute>
    <xsl:attribute name="space-after">1em</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="pre">
    <xsl:attribute name="font-size">0.83em</xsl:attribute>
    <xsl:attribute name="font-family"><xsl:value-of select="$MonospaceFont"/></xsl:attribute>
    <xsl:attribute name="white-space">pre</xsl:attribute>
    <xsl:attribute name="space-before">1em</xsl:attribute>
    <xsl:attribute name="space-after">1em</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="address">
    <xsl:attribute name="font-style">italic</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="hr">
    <xsl:attribute name="border">1px inset</xsl:attribute>
    <xsl:attribute name="space-before">0.67em</xsl:attribute>
    <xsl:attribute name="space-after">0.67em</xsl:attribute>
  </xsl:attribute-set>

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       List
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:attribute-set name="ul">
    <xsl:attribute name="space-before">1em</xsl:attribute>
    <xsl:attribute name="space-after">1em</xsl:attribute>
    <xsl:attribute name="provisional-distance-between-starts">2em</xsl:attribute>
    <xsl:attribute name="provisional-label-separation">0.125em</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="ul-nested">
    <xsl:attribute name="space-before">0pt</xsl:attribute>
    <xsl:attribute name="space-after">0pt</xsl:attribute>
    <xsl:attribute name="provisional-distance-between-starts">2em</xsl:attribute>
    <xsl:attribute name="provisional-label-separation">0.125em</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="ol">
    <xsl:attribute name="space-before">1em</xsl:attribute>
    <xsl:attribute name="space-after">1em</xsl:attribute>
    <xsl:attribute name="provisional-distance-between-starts">2em</xsl:attribute>
    <xsl:attribute name="provisional-label-separation">0.125em</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="ol-nested">
    <xsl:attribute name="space-before">0pt</xsl:attribute>
    <xsl:attribute name="space-after">0pt</xsl:attribute>
    <xsl:attribute name="provisional-distance-between-starts">2em</xsl:attribute>
    <xsl:attribute name="provisional-label-separation">0.125em</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="ul-li">
    <!-- for (unordered)fo:list-item -->
    <xsl:attribute name="relative-align">baseline</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="ol-li">
    <!-- for (ordered)fo:list-item -->
    <xsl:attribute name="relative-align">baseline</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="dl">
    <xsl:attribute name="space-before">1em</xsl:attribute>
    <xsl:attribute name="space-after">1em</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="dt">
    <xsl:attribute name="keep-with-next.within-column">always</xsl:attribute>
    <xsl:attribute name="keep-together.within-column">always</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="dd">
    <xsl:attribute name="start-indent">inherited-property-value(start-indent) + 24pt</xsl:attribute>
  </xsl:attribute-set>

  <!-- list-item-label format for each nesting level -->

  <xsl:param name="ul-label-1">&#x2022;</xsl:param>
  <xsl:attribute-set name="ul-label-1">
    <xsl:attribute name="font">1em <xsl:value-of select="$SerifFont"/></xsl:attribute>
  </xsl:attribute-set>

  <xsl:param name="ul-label-2">o</xsl:param>
  <xsl:attribute-set name="ul-label-2">
    <xsl:attribute name="font">0.67em <xsl:value-of select="$MonospaceFont"/></xsl:attribute>
    <xsl:attribute name="baseline-shift">0.25em</xsl:attribute>
  </xsl:attribute-set>

  <xsl:param name="ul-label-3">-</xsl:param>
  <xsl:attribute-set name="ul-label-3">
    <xsl:attribute name="font">bold 0.9em <xsl:value-of select="$SansSerifFont"/></xsl:attribute>
    <xsl:attribute name="baseline-shift">0.05em</xsl:attribute>
  </xsl:attribute-set>

  <xsl:param name="ol-label-1">1.</xsl:param>
  <xsl:attribute-set name="ol-label-1"/>

  <xsl:param name="ol-label-2">a.</xsl:param>
  <xsl:attribute-set name="ol-label-2"/>

  <xsl:param name="ol-label-3">i.</xsl:param>
  <xsl:attribute-set name="ol-label-3"/>
  
  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Table
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:attribute-set name="inside-table">
    <!-- prevent unwanted inheritance -->
    <xsl:attribute name="start-indent">0pt</xsl:attribute>
    <xsl:attribute name="end-indent">0pt</xsl:attribute>
    <xsl:attribute name="text-indent">0pt</xsl:attribute>
    <xsl:attribute name="last-line-end-indent">0pt</xsl:attribute>
    <xsl:attribute name="text-align">start</xsl:attribute>
    <xsl:attribute name="text-align-last">relative</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="table-and-caption" >
    <!-- horizontal alignment of table itself
    <xsl:attribute name="text-align">center</xsl:attribute>
    -->
    <!-- vertical alignment in table-cell -->
    <xsl:attribute name="display-align">center</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="table">
    <xsl:attribute name="border-collapse">separate</xsl:attribute>
    <xsl:attribute name="border-spacing">2px</xsl:attribute>
    <xsl:attribute name="border">1px solid</xsl:attribute>
    <!--
    <xsl:attribute name="border-style">outset</xsl:attribute>
    -->
  </xsl:attribute-set>

  <xsl:attribute-set name="table-caption" use-attribute-sets="inside-table">
    <xsl:attribute name="text-align">center</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="table-column">
  </xsl:attribute-set>

  <xsl:attribute-set name="thead" use-attribute-sets="inside-table">
  </xsl:attribute-set>

  <xsl:attribute-set name="tfoot" use-attribute-sets="inside-table">
  </xsl:attribute-set>

  <xsl:attribute-set name="tbody" use-attribute-sets="inside-table">
  </xsl:attribute-set>

  <xsl:attribute-set name="tr">
  </xsl:attribute-set>

  <xsl:attribute-set name="th">
    <xsl:attribute name="font-weight"><xsl:value-of select="$Bolder"/></xsl:attribute>
    <xsl:attribute name="text-align">center</xsl:attribute>
    <xsl:attribute name="border">1px solid</xsl:attribute>
    <!--
    <xsl:attribute name="border-style">inset</xsl:attribute>
    -->
    <xsl:attribute name="padding">1px</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="td">
    <xsl:attribute name="border">1px solid</xsl:attribute>
    <!--
    <xsl:attribute name="border-style">inset</xsl:attribute>
    -->
    <xsl:attribute name="padding">1px</xsl:attribute>
  </xsl:attribute-set>

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Inline-level
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:attribute-set name="b">
    <xsl:attribute name="font-weight"><xsl:value-of select="$Bolder"/></xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="strong">
    <xsl:attribute name="font-weight"><xsl:value-of select="$Bolder"/></xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="strong-em">
    <xsl:attribute name="font-weight"><xsl:value-of select="$Bolder"/></xsl:attribute>
    <xsl:attribute name="font-style">italic</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="i">
    <xsl:attribute name="font-style">italic</xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="cite">
    <xsl:attribute name="font-style">italic</xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="em">
    <xsl:attribute name="font-style">italic</xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="var">
    <xsl:attribute name="font-style">italic</xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="dfn">
    <xsl:attribute name="font-style">italic</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="tt">
    <xsl:attribute name="font-family"><xsl:value-of select="$MonospaceFont"/></xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="code">
    <xsl:attribute name="font-family"><xsl:value-of select="$MonospaceFont"/></xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="kbd">
    <xsl:attribute name="font-family"><xsl:value-of select="$MonospaceFont"/></xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="samp">
    <xsl:attribute name="font-family"><xsl:value-of select="$MonospaceFont"/></xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="big">
    <xsl:attribute name="font-size">larger</xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="small">
    <xsl:attribute name="font-size">smaller</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="sub">
    <xsl:attribute name="baseline-shift">sub</xsl:attribute>
    <xsl:attribute name="font-size">smaller</xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="sup">
    <xsl:attribute name="baseline-shift">super</xsl:attribute>
    <xsl:attribute name="font-size">smaller</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="s">
    <xsl:attribute name="text-decoration">line-through</xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="strike">
    <xsl:attribute name="text-decoration">line-through</xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="del">
    <xsl:attribute name="text-decoration">line-through</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="u">
    <xsl:attribute name="text-decoration">underline</xsl:attribute>
  </xsl:attribute-set>
  <xsl:attribute-set name="ins">
    <xsl:attribute name="text-decoration">underline</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="abbr">
    <!-- e.g.,
    <xsl:attribute name="font-variant">small-caps</xsl:attribute>
    <xsl:attribute name="letter-spacing">0.1em</xsl:attribute>
    -->
  </xsl:attribute-set>

  <xsl:attribute-set name="acronym">
    <!-- e.g.,
    <xsl:attribute name="font-variant">small-caps</xsl:attribute>
    <xsl:attribute name="letter-spacing">0.1em</xsl:attribute>
    -->
  </xsl:attribute-set>

  <xsl:attribute-set name="q"/>
  <xsl:attribute-set name="q-nested"/>

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Image
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:attribute-set name="img">
  </xsl:attribute-set>

  <xsl:attribute-set name="img-link">
    <xsl:attribute name="border">2px solid</xsl:attribute>
  </xsl:attribute-set>

  <xsl:attribute-set name="img-omitted">
    <xsl:attribute name="color">red</xsl:attribute>
    <xsl:attribute name="font-size">large</xsl:attribute>
    <xsl:attribute name="font-weight">bold</xsl:attribute>
    <xsl:attribute name="border">1px solid red</xsl:attribute>
  </xsl:attribute-set>
  
  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Link
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:attribute-set name="a-link">
    <xsl:attribute name="text-decoration">underline</xsl:attribute>
    <xsl:attribute name="color">blue</xsl:attribute>
  </xsl:attribute-set>


  <!--======================================================================
      Templates
  =======================================================================-->

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Root
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:template match="html:html">
    <fo:root xsl:use-attribute-sets="root">
      <xsl:call-template name="process-common-attributes"/>
      <xsl:call-template name="make-layout-master-set"/>
      <xsl:apply-templates/>
    </fo:root>
  </xsl:template>

  <xsl:template name="make-layout-master-set">
    <fo:layout-master-set>
      <fo:simple-page-master master-name="all-pages"
                             xsl:use-attribute-sets="page">
        <fo:region-body margin-top="{$page-margin-top}"
                        margin-right="{$page-margin-right}"
                        margin-bottom="{$page-margin-bottom}"
                        margin-left="{$page-margin-left}"
                        column-count="{$column-count}"
                        column-gap="{$column-gap}"/>
        <xsl:choose>
          <xsl:when test="$writing-mode = 'tb-rl'">
            <fo:region-before extent="{$page-margin-right}"
                              precedence="true"/>
            <fo:region-after  extent="{$page-margin-left}"
                              precedence="true"/>
            <fo:region-start  region-name="page-header"
                              extent="{$page-margin-top}"
                              writing-mode="lr-tb"
                              display-align="before"/>
            <fo:region-end    region-name="page-footer"
                              extent="{$page-margin-bottom}"
                              writing-mode="lr-tb"
                              display-align="after"/>
          </xsl:when>
          <xsl:when test="$writing-mode = 'rl-tb'">
            <fo:region-before region-name="page-header"
                              extent="{$page-margin-top}"
                              display-align="before"/>
            <fo:region-after  region-name="page-footer"
                              extent="{$page-margin-bottom}"
                              display-align="after"/>
            <fo:region-start  extent="{$page-margin-right}"/>
            <fo:region-end    extent="{$page-margin-left}"/>
          </xsl:when>
          <xsl:otherwise><!-- $writing-mode = 'lr-tb' -->
            <fo:region-before region-name="page-header"
                              extent="{$page-margin-top}"
                              display-align="before"/>
            <fo:region-after  region-name="page-footer"
                              extent="{$page-margin-bottom}"
                              display-align="after"/>
            <fo:region-start  extent="{$page-margin-left}"/>
            <fo:region-end    extent="{$page-margin-bottom}"/>
          </xsl:otherwise>
        </xsl:choose>
      </fo:simple-page-master>
    </fo:layout-master-set>
  </xsl:template>

  <xsl:template match="html:head | html:script"/>

  <xsl:template match="html:body">
    <fo:page-sequence master-reference="all-pages">
      <fo:title>
        <xsl:value-of select="/html:html/html:head/html:title"/>
      </fo:title>
      <fo:static-content flow-name="page-header">
        <fo:block space-before.conditionality="retain"
                  space-before="{$page-header-margin}"
                  xsl:use-attribute-sets="page-header">
          <xsl:if test="$title-print-in-header = 'true'">
            <xsl:value-of select="/html:html/html:head/html:title"/>
          </xsl:if>
        </fo:block>
      </fo:static-content>
      <fo:static-content flow-name="page-footer">
        <fo:block space-after.conditionality="retain"
                  space-after="{$page-footer-margin}"
                  xsl:use-attribute-sets="page-footer">
          <xsl:if test="$page-number-print-in-footer = 'true'">
            <xsl:text>- </xsl:text>
            <fo:page-number/>
            <xsl:text> -</xsl:text>
          </xsl:if>
        </fo:block>
      </fo:static-content>
      <fo:flow flow-name="xsl-region-body">
        <fo:block xsl:use-attribute-sets="body">
          <xsl:call-template name="process-common-attributes"/>
          <xsl:apply-templates/>
        </fo:block>
      </fo:flow>
    </fo:page-sequence>
  </xsl:template>
  
  <xsl:template match="html:style">
    <!-- do nothing, all styles should have been inlined before getting to this XSLT -->
  </xsl:template>

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
   process common attributes and children
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:template name="process-common-attributes-and-children">
    <xsl:call-template name="process-common-attributes"/>
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template name="process-common-attributes">
    <!-- TGH Add roles or tags as needed to define document structure; note that PDF tags are case-sensitive, unlike HTML tags -->
    <xsl:choose>
      <xsl:when test="('P','H1','H2','H3','H4','H5','H6') = fn:upper-case(local-name())">
        <xsl:call-template name="tag-element"><xsl:with-param name="tag" select="fn:upper-case(local-name())"/></xsl:call-template>
      </xsl:when>
      <xsl:when test="'BLOCKQUOTE' = fn:upper-case(local-name())">
        <xsl:call-template name="tag-element"><xsl:with-param name="tag" select="'BlockQuote'"/></xsl:call-template>
      </xsl:when>
      <xsl:when test="'DIV' = fn:upper-case(local-name())">
        <xsl:call-template name="tag-element"><xsl:with-param name="tag" select="'Div'"/></xsl:call-template>
      </xsl:when>
      <xsl:when test="'SPAN' = fn:upper-case(local-name())">
        <xsl:call-template name="tag-element"><xsl:with-param name="tag" select="'Span'"/></xsl:call-template>
      </xsl:when>
      <xsl:when test="'Q' = fn:upper-case(local-name())">
        <xsl:call-template name="tag-element"><xsl:with-param name="tag" select="'Quote'"/></xsl:call-template>
      </xsl:when>
    </xsl:choose>

    <xsl:choose>
      <!-- NOTE: The xml:lang attribute can interfer with Apache FOP's complex script functionality -->
      <xsl:when test="@xml:lang">
        <xsl:attribute name="xml:lang">
          <xsl:value-of select="@xml:lang"/>
        </xsl:attribute>
      </xsl:when>
      <xsl:when test="@lang">
        <xsl:attribute name="xml:lang">
          <xsl:value-of select="@lang"/>
        </xsl:attribute>
      </xsl:when>
    </xsl:choose>

    <xsl:choose>
      <!-- only create id attributes if they are unique with the whole document -->
      <!-- TODO: Eventually we should probably rewrite non-unique ids so they are unique -->
      <!--       This will require a bit of logoic to avoid breaking possible internal fragment id links -->
      <xsl:when test="@id and count(key('HTML_IDS',@id)) &lt;= 1">
        <xsl:attribute name="id">
          <xsl:value-of select="@id"/>
        </xsl:attribute>
      </xsl:when>
      <xsl:when test="self::html:a/@name and count(key('HTML_NAMES',self::html:a/@name)) &lt;= 1">
        <xsl:attribute name="id">
          <xsl:value-of select="@name"/>
        </xsl:attribute>
      </xsl:when>
    </xsl:choose>

    <xsl:if test="@align">
      <xsl:choose>
        <xsl:when test="self::html:caption">
        </xsl:when>
        <xsl:when test="self::html:img or self::html:object">
          <xsl:if test="fn:lower-case(@align) = 'bottom' or fn:lower-case(@align) = 'middle' or fn:lower-case(@align) = 'top'">
            <xsl:attribute name="vertical-align">
              <xsl:value-of select="fn:lower-case(@align)"/>
            </xsl:attribute>
          </xsl:if>
        </xsl:when>
        <xsl:otherwise>
          <xsl:call-template name="process-cell-align">
            <xsl:with-param name="align" select="fn:lower-case(@align)"/>
          </xsl:call-template>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
    <xsl:if test="@valign">
      <xsl:call-template name="process-cell-valign">
        <xsl:with-param name="valign" select="fn:lower-case(@valign)"/>
      </xsl:call-template>
    </xsl:if>
    
    <xsl:if test="@color">
      <xsl:attribute name="color">
        <xsl:value-of select="fn:lower-case(@color)"/>
      </xsl:attribute>      
    </xsl:if>

    <xsl:if test="@bgcolor">
      <xsl:attribute name="background-color">
        <xsl:value-of select="fn:lower-case(@bgcolor)"/>
      </xsl:attribute>      
    </xsl:if>

    <xsl:if test="@style">
      <xsl:call-template name="process-style">
        <xsl:with-param name="style" select="@style"/>
      </xsl:call-template>
    </xsl:if>
    
  </xsl:template>

  <xsl:template name="process-style">
    <xsl:param name="style"/>
    <!-- TODO:  The style might have a leading ; which should be removed -->  
    <!-- e.g., style="text-align: center; color: red"
         converted to text-align="center" color="red" -->
    <xsl:variable name="name"
                  select="fn:lower-case(normalize-space(substring-before($style, ':')))"/>
    <xsl:if test="$name">
      <xsl:variable name="value-and-rest"
                    select="normalize-space(substring-after($style, ':'))"/>
      <xsl:variable name="value">
        <xsl:choose>
          <xsl:when test="contains($value-and-rest, ';')">
            <xsl:value-of select="normalize-space(substring-before($value-and-rest, ';'))"/>
          </xsl:when>
          <xsl:otherwise>  
            <xsl:value-of select="$value-and-rest"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:variable>
      <xsl:choose>
        <xsl:when test="fn:starts-with(fn:normalize-space($value),'var(') or fn:starts-with(fn:normalize-space($value),'var (')">
          <!-- TGH Ignore any property with a var function -->
          <xsl:message>The CSS var function is not supported; dropping '<xsl:value-of select="$name"/>: <xsl:value-of select="$value"/>'</xsl:message>
        </xsl:when>
        <xsl:when test="$name = 'width' and (self::html:col or self::html:colgroup)">
          <xsl:attribute name="column-width">
            <xsl:value-of select="$value"/>
          </xsl:attribute>
        </xsl:when>
        
        <!-- TGH 2023-12-05 Make sure width and height of images do not exceed the page size of the expected PDF -->
        <xsl:when test="$name = 'width' and self::html:img">
          <xsl:attribute name="{$name}">
             <xsl:value-of select="my:ScaleToBodyWidth($value)"/>
          </xsl:attribute>
        </xsl:when>
        <xsl:when test="$name = 'height' and self::html:img">
          <xsl:attribute name="{$name}">
            <xsl:value-of select="my:ScaleToBodyHeight($value)"/>
          </xsl:attribute>
        </xsl:when>
        
        <!-- TGH 2024-07-22 html:a is translated to fo:basic-link, but some attributes are not supported -->
        
        <xsl:when test="$name='width' and self::html:a">
          <xsl:message>The basic-link does not support the <xsl:value-of select="$name"/>='<xsl:value-of select="$value"/>' attribute; it will be omitted.</xsl:message>
        </xsl:when>
        <xsl:when test="$name='height' and self::html:a">
          <xsl:message>The basic-link does not support the <xsl:value-of select="$name"/>='<xsl:value-of select="$value"/>' attribute; it will be omitted.</xsl:message>
        </xsl:when>
        <xsl:when test="$name='cursor'">
          <xsl:message>Property <xsl:value-of select="$name"/>='<xsl:value-of select="$value"/>' is not supported; it will be omitted.</xsl:message>
        </xsl:when>
        
        <xsl:when test="$name = 'vertical-align' and (
                                 self::html:table or self::html:caption or
                                 self::html:thead or self::html:tfoot or
                                 self::html:tbody or self::html:colgroup or
                                 self::html:col or self::html:tr or
                                 self::html:th or self::html:td)">
          <xsl:choose>
            <xsl:when test="$value = 'top'">
              <xsl:attribute name="display-align">before</xsl:attribute>
            </xsl:when>
            <xsl:when test="$value = 'bottom'">
              <xsl:attribute name="display-align">after</xsl:attribute>
            </xsl:when>
            <xsl:when test="$value = 'middle'">
              <xsl:attribute name="display-align">center</xsl:attribute>
            </xsl:when>
            <xsl:otherwise>
              <xsl:attribute name="display-align">auto</xsl:attribute>
              <xsl:attribute name="relative-align">baseline</xsl:attribute>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:when>
        <xsl:when test="$name = 'text-align'">
          <xsl:choose>
            <xsl:when test="$value = 'middle'">
              <!-- TGH Convert values of middle to values of center -->
              <xsl:attribute name="{$name}">center</xsl:attribute>     
            </xsl:when>
            <xsl:otherwise>
              <xsl:attribute name="{$name}">
                <xsl:value-of select="$value"/>
              </xsl:attribute>                          
            </xsl:otherwise>
          </xsl:choose>
        </xsl:when>
        <!-- TGH:  Apache FOP does not support bolder or lighter, so just use bold or normal -->
        <xsl:when test="$name = 'font-weight' and fn:lower-case(fn:normalize-space($value)) = 'bolder'">
          <xsl:attribute name="font-weight"><xsl:value-of select="$Bolder"/></xsl:attribute>
        </xsl:when>
        <xsl:when test="$name = 'font-weight' and fn:lower-case(fn:normalize-space($value)) = 'lighter'">
          <xsl:attribute name="font-weight"><xsl:value-of select="$Lighter"/></xsl:attribute>
        </xsl:when>
        <xsl:when test="$name = 'font-family'">
          <!-- TGH Map font family to one of the passed in parameters for fonts -->
          <xsl:attribute name="font-family">
            <xsl:call-template name="map-font-family">
              <xsl:with-param name="font-family" select="$value"/>
            </xsl:call-template>
          </xsl:attribute>
        </xsl:when>
        <xsl:when test="$name = 'font'">
          <!-- TGH Map font family to one of the passed in parameters for fonts -->
          <xsl:call-template name="append-font-family">
            <xsl:with-param name="font" select="$value"/>
          </xsl:call-template>          
        </xsl:when>
        <!-- TGH ignore some styles and transform some others -->
        <xsl:when test="$name = 'border-radius'">
          <!-- TGH Convert border-radius into the appropriate processor-specific extension property -->
          <xsl:choose>
            <xsl:when test="$fo-processor='fop'">
              <xsl:attribute name="fox:border-radius"><xsl:value-of select="$value"/></xsl:attribute>
            </xsl:when>
            <xsl:when test="$fo-processor='xep'">
              <xsl:attribute name="rx:border-radius"><xsl:value-of select="$value"/></xsl:attribute>              
            </xsl:when>
            <xsl:otherwise>
              <xsl:message>Unexpected FO processor '<xsl:value-of select="$fo-processor"/>'; dropping '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>'</xsl:message>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:when>
        
        <!-- TGH Convert overflow-x and/or overflow-y into the just overflow, unless there is already an overflow in which case just ignore them -->
        <xsl:when test="($name = 'overflow-x' or $name='overflow-y') and my:StyleContainsProperty(self::*/@style,'overflow')">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; there is already an 'overflow' property, so '<xsl:value-of select="$name"/>' will be omitted.</xsl:message>          
        </xsl:when>
        <xsl:when test="$name = 'overflow-x' and not(my:StyleContainsProperty(self::*/@style,'overflow'))">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; it is converted to a plain 'overflow:<xsl:value-of select="$value"/>'</xsl:message>
          <xsl:attribute name="overflow"><xsl:value-of select="$value"/></xsl:attribute>
        </xsl:when>
        <xsl:when test="$name = 'overflow-y' and not(my:StyleContainsProperty(self::*/@style,'overflow')) and my:StyleContainsProperty(self::*/@style,'overflow-x')">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; there was an 'overflow-x' already converted to an 'overflow' property, so the '<xsl:value-of select="$name"/>' will be omitted</xsl:message>
        </xsl:when>
        <xsl:when test="$name = 'overflow-y' and not(my:StyleContainsProperty(self::*/@style,'overflow'))">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; it is converted to a plain 'overflow:<xsl:value-of select="$value"/>'</xsl:message>
          <xsl:attribute name="overflow"><xsl:value-of select="$value"/></xsl:attribute>
        </xsl:when>
        
        <xsl:when test="fn:starts-with($name,'border-image')">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; it was dropped.</xsl:message>
          <!-- FUTURE: Might be able replicate this with some XSL-FO extensions -->
        </xsl:when>
        
        <xsl:when test="$name = 'overflow-wrap'">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; it was dropped.</xsl:message>
          <!-- FUTURE: When value is 'anywhere' or 'break-word' might be able to fake this by insert zwnj characters between characters -->
        </xsl:when>
        
        <xsl:when test="$name = 'list-style-type'">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; it was dropped.</xsl:message>
        </xsl:when>
        <xsl:when test="$name = 'display'">
          <!-- FUTURE:  May be able to convert some display property values, see the XEP xeponline-fo-translate-2.xsl stylesheet -->
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; it was dropped.</xsl:message>
        </xsl:when>
        <xsl:when test="$name = 'flex-wrap'">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; it was dropped.</xsl:message>
        </xsl:when>
        <xsl:when test="$name = 'text-decoration-style'">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; it was dropped.</xsl:message>
        </xsl:when>
        <xsl:when test="$name = 'text-decoration-color'">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; it was dropped.</xsl:message>
        </xsl:when>
        <xsl:when test="$name = 'align-items'">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; it was dropped.</xsl:message>
        </xsl:when>
        <xsl:when test="$name = 'text-decoration-line'">
          <xsl:message>Property '<xsl:value-of select="$name"/>:<xsl:value-of select="$value"/>' is not supported; it was converted to 'text-decoration:<xsl:value-of select="$value"/>'.</xsl:message>
          <xsl:attribute name="text-decoration">
            <xsl:value-of select="$value"/>
          </xsl:attribute>            
        </xsl:when>
        <xsl:otherwise>
              <xsl:attribute name="{$name}">
                <xsl:value-of select="$value"/>
              </xsl:attribute>            
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
    <xsl:variable name="rest"
                  select="normalize-space(substring-after($style, ';'))"/>
    <xsl:if test="$rest">
      <xsl:call-template name="process-style">
        <xsl:with-param name="style" select="$rest"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>


  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Block-level
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:template match="html:h1">
    <fo:block xsl:use-attribute-sets="h1">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:h2">
    <fo:block xsl:use-attribute-sets="h2">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:h3">
    <fo:block xsl:use-attribute-sets="h3">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:h4">
    <fo:block xsl:use-attribute-sets="h4">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:h5">
    <fo:block xsl:use-attribute-sets="h5">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:h6">
    <fo:block xsl:use-attribute-sets="h6">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:p">
    <fo:block xsl:use-attribute-sets="p">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <!-- initial paragraph, preceded by h1..6 or div -->
  <xsl:template match="html:p[preceding-sibling::*[1][
                       self::html:h1 or self::html:h2 or self::html:h3 or
                       self::html:h4 or self::html:h5 or self::html:h6 or
                       self::html:div]]">
    <fo:block xsl:use-attribute-sets="p-initial">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <!-- initial paragraph, first child of div, body or td -->
  <xsl:template match="html:p[not(preceding-sibling::*) and (
                       parent::html:div or parent::html:body or
                       parent::html:td)]">
    <fo:block xsl:use-attribute-sets="p-initial-first">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:blockquote">
    <fo:block xsl:use-attribute-sets="blockquote">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:pre">
    <fo:block xsl:use-attribute-sets="pre">
      <xsl:call-template name="process-pre"/>
    </fo:block>
  </xsl:template>
  
  <!-- Add a tag attribute to the element --> 
  <xsl:template name="tag-element">
    <xsl:param name="tag">Div</xsl:param>
    <xsl:attribute name="role"><xsl:value-of select="$tag"/></xsl:attribute>
    <xsl:if test="$fo-processor='xep'">
      <xsl:attribute name="rx:pdf-structure-tag"><xsl:value-of select="$tag"/></xsl:attribute>          
    </xsl:if>
  </xsl:template>
  
  <!-- Add an artifact tag to the element -->
  <xsl:template name="tag-artifact">
    <xsl:param name="type"></xsl:param>
    <xsl:param name="subtype"></xsl:param>
    <xsl:choose>
      <xsl:when test="$fo-processor='xep'">
        <xsl:attribute name="role"><xsl:value-of select="'Artifact'"/></xsl:attribute>          
        <xsl:attribute name="rx:pdf-structure-tag"><xsl:value-of select="'Artifact'"/></xsl:attribute>          
        <xsl:if test="$type"><!-- See RenderX docs:  "Pagination", "Page" or "Layout". -->
          <xsl:attribute name="rx:pdf-artifact-type"><xsl:value-of select="$type"/></xsl:attribute>                    
        </xsl:if>
        <xsl:if test="$subtype"><!-- See RenderX docs:  Only applies to "Pagination": "Header", "Footer" or "Watermark". -->
          <xsl:attribute name="rx:pdf-artifact-subtype"><xsl:value-of select="$subtype"/></xsl:attribute>                    
        </xsl:if>
      </xsl:when>
      <xsl:otherwise>
        <xsl:attribute name="role"><xsl:value-of select="'artifact'"/></xsl:attribute>                  
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <!-- Shortcuts for tagging elements -->
  <xsl:template name="tag-Document">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">Document</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-Sect">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">Sect</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-Part">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">Part</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-Art">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">Art</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-L">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">L</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-LI">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">LI</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-Lbl">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">Lbl</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-LBody">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">LBody</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-H1">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">H1</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-H2">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">H2</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-H3">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">H3</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-H4">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">H4</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-H5">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">H5</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-H6">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">H6</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-P">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">P</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-Span">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">Span</xsl:with-param></xsl:call-template>
  </xsl:template>
  <xsl:template name="tag-Div">
    <xsl:call-template name="tag-element"><xsl:with-param name="tag">Div</xsl:with-param></xsl:call-template>
  </xsl:template>
  

  <xsl:template name="process-pre">
    <xsl:call-template name="process-common-attributes"/>
    <!-- remove leading CR/LF/CRLF char -->
    <xsl:variable name="crlf"><xsl:text>&#xD;&#xA;</xsl:text></xsl:variable>
    <xsl:variable name="lf"><xsl:text>&#xA;</xsl:text></xsl:variable>
    <xsl:variable name="cr"><xsl:text>&#xD;</xsl:text></xsl:variable>
    <xsl:for-each select="node()">
      <xsl:choose>
        <xsl:when test="position() = 1 and self::text()">
          <xsl:choose>
            <xsl:when test="starts-with(., $lf)">
              <xsl:value-of select="substring(., 2)"/>
            </xsl:when>
            <xsl:when test="starts-with(., $crlf)">
              <xsl:value-of select="substring(., 3)"/>
            </xsl:when>
            <xsl:when test="starts-with(., $cr)">
              <xsl:value-of select="substring(., 2)"/>
            </xsl:when>
            <xsl:otherwise>
              <xsl:apply-templates select="."/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:when>
        <xsl:otherwise>
          <xsl:apply-templates select="."/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="html:address">
    <fo:block xsl:use-attribute-sets="address">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:hr">
    <fo:block xsl:use-attribute-sets="hr">
      <xsl:call-template name="process-common-attributes"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:div">
    <!-- need fo:block-container? or normal fo:block -->
    <xsl:variable name="need-block-container">
      <xsl:call-template name="need-block-container"/>
    </xsl:variable>
    <xsl:choose>
      <xsl:when test="$need-block-container = 'true'">
        <fo:block-container>
          <xsl:if test="@dir">
            <xsl:attribute name="writing-mode">
              <xsl:choose>
                <xsl:when test="fn:lower-case(fn:normalize-space(@dir)) = 'rtl'">rl-tb</xsl:when>
                <xsl:otherwise>lr-tb</xsl:otherwise>
              </xsl:choose>
            </xsl:attribute>
          </xsl:if>
          <xsl:call-template name="process-common-attributes"/>
          <fo:block start-indent="0pt" end-indent="0pt">
            <xsl:apply-templates/>
          </fo:block>
        </fo:block-container>
      </xsl:when>
      <xsl:otherwise>
        <!-- normal block -->
        <fo:block>
          <xsl:call-template name="process-common-attributes"/>
          <xsl:apply-templates/>
        </fo:block>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="need-block-container">
    <xsl:choose>
      <xsl:when test="@dir">true</xsl:when>
      <xsl:when test="@style">
        <xsl:variable name="s"
                      select="concat(';', translate(normalize-space(@style),
                                                    ' ', ''))"/>
        <xsl:choose>
          <xsl:when test="contains($s, ';width:') or
                          contains($s, ';height:') or
                          contains($s, ';position:absolute') or
                          contains($s, ';position:fixed') or
                          contains($s, ';writing-mode:')">true</xsl:when>
          <xsl:otherwise>false</xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:otherwise>false</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="html:center">
    <fo:block text-align="center">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:fieldset | html:dir | html:menu">
    <fo:block space-before="1em" space-after="1em">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>
  
  <xsl:template match="html:form">
    <xsl:choose>
      <xsl:when test="parent::html:tr and (html:td or html:th)">
        <!-- TGH form is a child of tr and has td or th children -->
        <xsl:comment>WARNING: A form child of the tr was removed.</xsl:comment>
        <xsl:apply-templates/>
      </xsl:when>
      <xsl:otherwise>
        <fo:block space-before="1em" space-after="1em">
          <xsl:call-template name="process-common-attributes-and-children"/>
        </fo:block>        
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       List
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:template match="html:ul">
    <fo:list-block xsl:use-attribute-sets="ul">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:list-block>
  </xsl:template>

  <xsl:template match="html:li//html:ul">
    <fo:list-block xsl:use-attribute-sets="ul-nested">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:list-block>
  </xsl:template>

  <xsl:template match="html:ol">
    <fo:list-block xsl:use-attribute-sets="ol">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:list-block>
  </xsl:template>

  <xsl:template match="html:li//html:ol">
    <fo:list-block xsl:use-attribute-sets="ol-nested">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:list-block>
  </xsl:template>

  <xsl:template match="html:ul/html:li">
    <fo:list-item xsl:use-attribute-sets="ul-li">
      <xsl:call-template name="process-ul-li"/>
    </fo:list-item>
  </xsl:template>

  <!-- TODO: Need to suppress the label if this is a nested list -->
  
  <xsl:template name="process-ul-li">
    <xsl:call-template name="process-common-attributes"/>
    <fo:list-item-label end-indent="label-end()"
                        text-align="end" wrap-option="no-wrap">
      <fo:block>
        <xsl:variable name="depth" select="count(ancestor::html:ul)" />
        <xsl:choose>
          <xsl:when test="$depth = 1">
            <fo:inline xsl:use-attribute-sets="ul-label-1">
              <xsl:value-of select="$ul-label-1"/>
            </fo:inline>
          </xsl:when>
          <xsl:when test="$depth = 2">
            <fo:inline xsl:use-attribute-sets="ul-label-2">
              <xsl:value-of select="$ul-label-2"/>
            </fo:inline>
          </xsl:when>
          <xsl:otherwise>
            <fo:inline xsl:use-attribute-sets="ul-label-3">
              <xsl:value-of select="$ul-label-3"/>
            </fo:inline>
          </xsl:otherwise>
        </xsl:choose>
      </fo:block>
    </fo:list-item-label>
    <fo:list-item-body start-indent="body-start()">
      <fo:block>
        <xsl:apply-templates/>
      </fo:block>
    </fo:list-item-body>
  </xsl:template>

  <xsl:template match="html:ol/html:li">
    <fo:list-item xsl:use-attribute-sets="ol-li">
      <xsl:call-template name="process-ol-li"/>
    </fo:list-item>
  </xsl:template>

  <xsl:template name="process-ol-li">
    <xsl:call-template name="process-common-attributes"/>
    <fo:list-item-label end-indent="label-end()"
                        text-align="end" wrap-option="no-wrap">
      <fo:block>
        <xsl:variable name="depth" select="count(ancestor::html:ol)" />
        <xsl:choose>
          <xsl:when test="$depth = 1">
            <fo:inline xsl:use-attribute-sets="ol-label-1">
              <xsl:number format="{$ol-label-1}"/>
            </fo:inline>
          </xsl:when>
          <xsl:when test="$depth = 2">
            <fo:inline xsl:use-attribute-sets="ol-label-2">
              <xsl:number format="{$ol-label-2}"/>
            </fo:inline>
          </xsl:when>
          <xsl:otherwise>
            <fo:inline xsl:use-attribute-sets="ol-label-3">
              <xsl:number format="{$ol-label-3}"/>
            </fo:inline>
          </xsl:otherwise>
        </xsl:choose>
      </fo:block>
    </fo:list-item-label>
    <fo:list-item-body start-indent="body-start()">
      <fo:block>
        <xsl:apply-templates/>
      </fo:block>
    </fo:list-item-body>
  </xsl:template>

  <!-- TODO: Convert DL into an FO List -->
  <xsl:template match="html:dl">
    <fo:block xsl:use-attribute-sets="dl">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:dt">
    <fo:block xsl:use-attribute-sets="dt">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <xsl:template match="html:dd">
    <fo:block xsl:use-attribute-sets="dd">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:block>
  </xsl:template>

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Table
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:template match="html:table[*]"><!-- TGH only process tables which have children  -->
    <xsl:choose>
      <xsl:when test="$fo-processor = 'fop'"> <!-- apache fop does no support table-and-caption -->
        <xsl:choose>
          <xsl:when test="parent::html:table or parent::html:tbody"><!-- TGH If the parent of this table is another table or tbody, just ignore it and process its children-->
            <xsl:comment>WARNING: Extraneous table element started here, but was removed</xsl:comment>
            <xsl:choose>
              <xsl:when test="html:tbody">
                <xsl:apply-templates select="html:tbody/*"/>
              </xsl:when>
              <xsl:otherwise>
                <xsl:apply-templates/>               
              </xsl:otherwise>
            </xsl:choose>
            <xsl:comment>WARNING: Extraneous table element that was removed ended here</xsl:comment>            
          </xsl:when>
          <xsl:otherwise>
            <fo:table xsl:use-attribute-sets="table">
              <!-- TODO: Process table caption using different markup than fo:table-and-caption-->
              <xsl:call-template name="process-table"/>
            </fo:table>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>  
      <xsl:otherwise>
        <xsl:choose>
          <xsl:when test="parent::html:table or parent::html:tbody"><!-- TGH If the parent of this table is another table or tbody, just ignore it and process its children-->
            <xsl:comment>WARNING: Extraneous table element started here, but was removed</xsl:comment>
            <xsl:choose>
              <xsl:when test="html:tbody">
                <xsl:apply-templates select="html:tbody/*"/>
              </xsl:when>
              <xsl:otherwise>
                <xsl:apply-templates/>               
              </xsl:otherwise>
            </xsl:choose>
            <xsl:comment>WARNING: Extraneous table element that was removed ended here</xsl:comment>                        
          </xsl:when>
          <xsl:otherwise>
            <fo:table-and-caption xsl:use-attribute-sets="table-and-caption">
              <xsl:call-template name="make-table-caption"/>
              <fo:table xsl:use-attribute-sets="table">
                <xsl:call-template name="process-table"/>
              </fo:table>
            </fo:table-and-caption>                    
          </xsl:otherwise>
        </xsl:choose>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="make-table-caption">
    <xsl:if test="html:caption/@align">
      <xsl:attribute name="caption-side">
        <xsl:value-of select="html:caption/@align"/>
      </xsl:attribute>
    </xsl:if>
    <xsl:apply-templates select="html:caption"/>
  </xsl:template>

  <xsl:template name="process-table">
    <xsl:if test="@width">
      <xsl:attribute name="inline-progression-dimension">
        <xsl:choose>
          <xsl:when test="contains(@width, '%')">
            <xsl:value-of select="@width"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="@width"/>px</xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
    </xsl:if>
    <xsl:if test="@border or @frame">
      <xsl:choose>
        <xsl:when test="number(@border) != number(@border)"><!-- TGH Test for non-standard value for HTML -->
          <xsl:attribute name="border"><xsl:value-of select="@border"/></xsl:attribute>
          <xsl:message terminate="no">Attribute border='<xsl:value-of select="@border"/>' is not a number; using it as is in the FO</xsl:message>
        </xsl:when>
        <xsl:when test="@border &gt; 0">
          <xsl:attribute name="border"><xsl:value-of select="@border"/>px solid</xsl:attribute>
        </xsl:when>
      </xsl:choose>
      <xsl:choose>
        <xsl:when test="@border = '0' or @frame = 'void'">
          <xsl:attribute name="border-style">hidden</xsl:attribute>
        </xsl:when>
        <xsl:when test="@frame = 'above'">
          <xsl:attribute name="border-style">outset hidden hidden hidden</xsl:attribute>
        </xsl:when>
        <xsl:when test="@frame = 'below'">
          <xsl:attribute name="border-style">hidden hidden outset hidden</xsl:attribute>
        </xsl:when>
        <xsl:when test="@frame = 'hsides'">
          <xsl:attribute name="border-style">outset hidden</xsl:attribute>
        </xsl:when>
        <xsl:when test="@frame = 'vsides'">
          <xsl:attribute name="border-style">hidden outset</xsl:attribute>
        </xsl:when>
        <xsl:when test="@frame = 'lhs'">
          <xsl:attribute name="border-style">hidden hidden hidden outset</xsl:attribute>
        </xsl:when>
        <xsl:when test="@frame = 'rhs'">
          <xsl:attribute name="border-style">hidden outset hidden hidden</xsl:attribute>
        </xsl:when>
        <xsl:otherwise>
          <xsl:attribute name="border-style">outset</xsl:attribute>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
    <xsl:if test="@cellspacing">
      <xsl:attribute name="border-spacing">
        <xsl:value-of select="@cellspacing"/>px</xsl:attribute>
      <xsl:attribute name="border-collapse">separate</xsl:attribute>
    </xsl:if>
    <xsl:if test="@rules and (@rules = 'groups' or
                      @rules = 'rows' or
                      @rules = 'cols' or
                      @rules = 'all' and (not(@border or @frame) or
                          @border = '0' or @frame and
                          not(@frame = 'box' or @frame = 'border')))">
      <xsl:attribute name="border-collapse">collapse</xsl:attribute>
      <xsl:if test="not(@border or @frame)">
        <xsl:attribute name="border-style">hidden</xsl:attribute>
      </xsl:if>
    </xsl:if>
    <xsl:call-template name="process-common-attributes"/>
    <xsl:apply-templates select="html:col | html:colgroup"/>
    <xsl:apply-templates select="html:thead"/>
    <xsl:apply-templates select="html:tfoot"/>
    <xsl:choose>
      <xsl:when test="html:tbody">
        <xsl:apply-templates select="html:tbody"/>
      </xsl:when>
      <xsl:otherwise>
        <fo:table-body xsl:use-attribute-sets="tbody">
          <xsl:apply-templates select="html:tr"/>
        </fo:table-body>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="html:caption">
    <fo:table-caption xsl:use-attribute-sets="table-caption">
      <xsl:call-template name="process-common-attributes"/>
      <fo:block>
        <xsl:apply-templates/>
      </fo:block>
    </fo:table-caption>
  </xsl:template>

  <xsl:template match="html:thead">
    <fo:table-header xsl:use-attribute-sets="thead">
      <xsl:call-template name="process-table-rowgroup"/>
    </fo:table-header>
  </xsl:template>

  <xsl:template match="html:tfoot">
    <fo:table-footer xsl:use-attribute-sets="tfoot">
      <xsl:call-template name="process-table-rowgroup"/>
    </fo:table-footer>
  </xsl:template>

  <xsl:template match="html:tbody">
    <xsl:choose>
      <xsl:when test="*"><!-- TGH If the tbody has children -->
        <xsl:choose>
          <xsl:when test="parent::html:tbody"><!-- TGH if the parent of this tbody is another tbody, just ignore it and keep processing its children-->
            <xsl:comment>WARNING: Extraneous tbody element started here, but was removed</xsl:comment>
            <xsl:apply-templates/>
            <xsl:comment>WARNING: Extraneous tbody element that was removed ended here</xsl:comment>
          </xsl:when>
          <xsl:otherwise>
            <fo:table-body xsl:use-attribute-sets="tbody">
              <xsl:call-template name="process-table-rowgroup"/>
            </fo:table-body>        
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:otherwise>
        <fo:table-body xsl:use-attribute-sets="tbody">
          <fo:table-cell><fo:block><xsl:comment>WARNING: Empty Table</xsl:comment></fo:block></fo:table-cell>
        </fo:table-body>        
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="process-table-rowgroup">
    <xsl:if test="ancestor::html:table[1]/@rules = 'groups'">
      <xsl:attribute name="border">1px solid</xsl:attribute>
    </xsl:if>
    <xsl:call-template name="process-common-attributes-and-children"/>
  </xsl:template>

  <xsl:template match="html:colgroup">
    <fo:table-column xsl:use-attribute-sets="table-column">
      <xsl:call-template name="process-table-column"/>
    </fo:table-column>
  </xsl:template>

  <xsl:template match="html:colgroup[html:col]">
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="html:col">
    <fo:table-column xsl:use-attribute-sets="table-column">
      <xsl:call-template name="process-table-column"/>
    </fo:table-column>
  </xsl:template>

  <xsl:template name="process-table-column">
    <xsl:if test="parent::html:colgroup">
      <xsl:call-template name="process-col-width">
        <xsl:with-param name="width" select="../@width"/>
      </xsl:call-template>
      <xsl:call-template name="process-cell-align">
        <xsl:with-param name="align" select="../@align"/>
      </xsl:call-template>
      <xsl:call-template name="process-cell-valign">
        <xsl:with-param name="valign" select="../@valign"/>
      </xsl:call-template>
    </xsl:if>
    <xsl:if test="@span">
      <xsl:attribute name="number-columns-repeated">
        <xsl:value-of select="@span"/>
      </xsl:attribute>
    </xsl:if>
    <xsl:call-template name="process-col-width">
      <xsl:with-param name="width" select="@width"/>
      <!-- it may override parent colgroup's width -->
    </xsl:call-template>
    <xsl:if test="ancestor::html:table[1]/@rules = 'cols'">
      <xsl:attribute name="border">1px solid</xsl:attribute>
    </xsl:if>
    <xsl:call-template name="process-common-attributes"/>
    <!-- this processes also align and valign -->
  </xsl:template>

  <xsl:template match="html:tr">
    <fo:table-row xsl:use-attribute-sets="tr">
      <xsl:call-template name="process-table-row"/>
    </fo:table-row>
  </xsl:template>

  <xsl:template match="html:tr[parent::html:table and html:th and not(html:td)]">
    <fo:table-row xsl:use-attribute-sets="tr" keep-with-next="always">
      <xsl:call-template name="process-table-row"/>
    </fo:table-row>
  </xsl:template>

  <xsl:template name="process-table-row">
    <xsl:if test="ancestor::html:table[1]/@rules = 'rows'">
      <xsl:attribute name="border">1px solid</xsl:attribute>
    </xsl:if>
    <xsl:call-template name="process-common-attributes-and-children"/>
  </xsl:template>

  <xsl:template match="html:th">
    <xsl:choose>
      <!-- also need to accomodate oddball cases like <table><tr><form><td> where the immediate child of tr may not be a td or th -->
      <xsl:when test="not(parent::html:tr or parent::*/parent::html:tr)">
        <!-- does not have a tr parent, so just treat it as a block -->
        <xsl:comment>WARNING: th element without a tr parent is treated as a block element</xsl:comment>
        <fo:block xsl:use-attribute-sets="td">
          <xsl:apply-templates/>
        </fo:block>                
      </xsl:when>
      <xsl:otherwise>
        <fo:table-cell xsl:use-attribute-sets="th">
          <xsl:call-template name="process-table-cell"/>
        </fo:table-cell>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="html:td">
    <xsl:choose>
      <!-- also need to accomodate oddball cases like <table><tr><form><td> where the immediate child of tr may not be a td or th -->
      <xsl:when test="not(parent::html:tr or parent::*/parent::html:tr)">
        <!-- does not have a tr parent, so just treat it as a block -->
        <xsl:comment>WARNING: td element without a tr parent is treated as a block element</xsl:comment>
        <fo:block xsl:use-attribute-sets="td">
          <xsl:apply-templates/>
        </fo:block>                
      </xsl:when>
      <xsl:otherwise>
        <fo:table-cell xsl:use-attribute-sets="td">
          <xsl:call-template name="process-table-cell"/>
        </fo:table-cell>        
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  
  <xsl:template name="process-table-cell">
    <xsl:variable name="row-count" select="count(../../html:tr)"/>
     <xsl:if test="@colspan">
      <xsl:attribute name="number-columns-spanned">
        <xsl:value-of select="@colspan"/>
      </xsl:attribute>
    </xsl:if>
    <xsl:if test="@rowspan">
      <xsl:variable name="preceding-row-count" select="count(parent::html:tr/preceding-sibling::html:tr)"/>
      <xsl:choose>
        <xsl:when test="($preceding-row-count + @rowspan) &gt; $row-count">
          <!-- rowspan goes beyond the last row in the table, so shorten it, assuming the intention was to use all the remaining rows -->
          <!-- based on answer to https://stackoverflow.com/questions/41123392/detecting-table-element-error-in-docbook-5-0-document -->
          <xsl:attribute name="number-rows-spanned">
            <xsl:value-of select="$row-count - $preceding-row-count"/>
          </xsl:attribute>
          <!-- use xml:comment to sneak in debug info w/o parsers complaining -->
          <!-- <xsl:attribute name="xml:comment">CurrentRow-<xsl:value-of select="$preceding-row-count + 1"/>, OrigRowSpan-<xsl:value-of select="@rowspan"/>, RowCount-<xsl:value-of select="$row-count"/></xsl:attribute> -->
        </xsl:when>
        <xsl:otherwise>
          <xsl:attribute name="number-rows-spanned">
            <xsl:value-of select="@rowspan"/>
          </xsl:attribute>          
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
    <xsl:for-each select="ancestor::html:table[1]">
      <xsl:if test="(@border or @rules) and (@rules = 'all' or
                    not(@rules) and not(@border = '0'))">
        <xsl:attribute name="border-style">inset</xsl:attribute>
      </xsl:if>
      <xsl:if test="@cellpadding">
        <xsl:attribute name="padding">
          <xsl:choose>
            <xsl:when test="contains(@cellpadding, '%')">
              <xsl:value-of select="@cellpadding"/>
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="@cellpadding"/>px</xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
      </xsl:if>
    </xsl:for-each>
    <xsl:if test="not(@align or ../@align or
                      ../parent::*[self::html:thead or self::html:tfoot or
                      self::html:tbody]/@align) and
                  ancestor::html:table[1]/*[self::html:col or
                      self::html:colgroup]/descendant-or-self::*/@align">
      <xsl:attribute name="text-align">from-table-column()</xsl:attribute>
    </xsl:if>
    <xsl:if test="not(@valign or ../@valign or
                      ../parent::*[self::html:thead or self::html:tfoot or
                      self::html:tbody]/@valign) and
                  ancestor::html:table[1]/*[self::html:col or
                      self::html:colgroup]/descendant-or-self::*/@valign">
      <xsl:attribute name="display-align">from-table-column()</xsl:attribute>
      <xsl:attribute name="relative-align">from-table-column()</xsl:attribute>
    </xsl:if>
    <!-- TGH Add the width and height attribute -->
    <!-- TODO:  FOP sometimes generates a warning: "ERROR - Cannot find LM to handle given FO for LengthBase. (fo:table-cell, location: row:col)" -->
    <!--        Seems to be associated with cell width attribute.  Maybe on nested tables??? -->
    <xsl:choose>
      <xsl:when test="string(number(@width)) != 'NaN'">
        <xsl:attribute name="width"><xsl:value-of select="@width"/>px</xsl:attribute>
      </xsl:when>
      <xsl:when test="@width">
        <xsl:attribute name="width"><xsl:value-of select="@width"/></xsl:attribute>
      </xsl:when>
    </xsl:choose>
    <xsl:choose>
      <xsl:when test="string(number(@height)) != 'NaN'">
        <xsl:attribute name="height"><xsl:value-of select="@height"/>px</xsl:attribute>
      </xsl:when>
      <xsl:when test="@height">
        <xsl:attribute name="height"><xsl:value-of select="@height"/></xsl:attribute>
      </xsl:when>
    </xsl:choose>
    <xsl:call-template name="process-common-attributes"/>
    <fo:block>
      <xsl:apply-templates/>
    </fo:block>
  </xsl:template>

  <xsl:template name="process-col-width">
    <xsl:param name="width"/>
    <xsl:if test="$width and $width != '0*'">
      <xsl:attribute name="column-width">
        <xsl:choose>
          <xsl:when test="contains($width, '*')">
            <xsl:text>proportional-column-width(</xsl:text>
            <xsl:value-of select="substring-before($width, '*')"/>
            <xsl:text>)</xsl:text>
          </xsl:when>
          <xsl:when test="contains($width, '%')">
            <xsl:value-of select="$width"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="$width"/>px</xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
    </xsl:if>
  </xsl:template>

  <xsl:template name="process-cell-align">
    <xsl:param name="align"/>
    <xsl:if test="$align">
      <xsl:attribute name="text-align">
        <xsl:choose>
          <xsl:when test="$align = 'char'">
            <xsl:choose>
              <xsl:when test="$align/../@char">
                <xsl:value-of select="$align/../@char"/>
              </xsl:when>
              <xsl:otherwise>
                <xsl:value-of select="'.'"/>
                <!-- TODO: it should depend on xml:lang ... -->
              </xsl:otherwise>
            </xsl:choose>
          </xsl:when>
          <xsl:otherwise>
			     <!-- TGH convert alignment values to lower-case -->
            <xsl:choose>
              <xsl:when test="fn:lower-case($align)='middle'">
                <!-- TGH should be center -->
                <xsl:text>center</xsl:text>
              </xsl:when>
              <xsl:otherwise>
                <xsl:value-of select="fn:lower-case($align)"/>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
    </xsl:if>
  </xsl:template>

  <xsl:template name="process-cell-valign">
    <xsl:param name="valign"/>
    <xsl:if test="$valign">
      <xsl:attribute name="display-align">
        <xsl:choose>
          <xsl:when test="fn:lower-case($valign) = 'middle'">center</xsl:when>
          <xsl:when test="fn:lower-case($valign) = 'bottom'">after</xsl:when>
          <xsl:when test="fn:lower-case($valign) = 'baseline'">auto</xsl:when>
          <xsl:otherwise>before</xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <xsl:if test="fn:lower-case($valign) = 'baseline'">
        <xsl:attribute name="relative-align">baseline</xsl:attribute>
      </xsl:if>
    </xsl:if>
  </xsl:template>

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Inline-level
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->
  
  <!-- TGH Add support for <font> element -->
  <xsl:template match="html:font">
    <fo:inline>
      <xsl:call-template name="process-font-attributes"/>
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>
  
  <xsl:template name="process-font-attributes">
    <xsl:choose>
      <xsl:when test="normalize-space(@size)='1'">
        <xsl:attribute name="font-size">xx-small</xsl:attribute>
      </xsl:when>
      <xsl:when test="normalize-space(@size)='2'">
        <xsl:attribute name="font-size">small</xsl:attribute>
      </xsl:when>
      <xsl:when test="normalize-space(@size)='3'">
        <xsl:attribute name="font-size">medium</xsl:attribute>
      </xsl:when>
      <xsl:when test="normalize-space(@size)='4'">
        <xsl:attribute name="font-size">large</xsl:attribute>
      </xsl:when>
      <xsl:when test="normalize-space(@size)='5'">
        <xsl:attribute name="font-size">x-large</xsl:attribute>
      </xsl:when>
      <xsl:when test="normalize-space(@size)='6'">
        <xsl:attribute name="font-size">xx-large</xsl:attribute>
      </xsl:when>
      <xsl:when test="normalize-space(@size)='7'">
        <xsl:attribute name="font-size">xx-large</xsl:attribute>
      </xsl:when>
      <!-- relative sizes, just increases or decreases, doesn't support a numeric value -->
      <xsl:when test="starts-with(normalize-space(@size),'+')">
        <xsl:attribute name="font-size">larger</xsl:attribute>        
      </xsl:when>
      <xsl:when test="starts-with(normalize-space(@size),'-')">
        <xsl:attribute name="font-size">smaller</xsl:attribute>        
      </xsl:when>
    </xsl:choose>
    <xsl:if test="@face">
      <xsl:attribute name="font-family">
        <xsl:call-template name="map-font-family">
          <xsl:with-param name="font-family" select="@face"/>
        </xsl:call-template>
      </xsl:attribute>
    </xsl:if>
  </xsl:template>
  
  <!-- TGH Map font-family or face into one of the predefined values for SerifFont, SansSerifFont, or MonospaceFont -->
  <xsl:template name="map-font-family">
    <xsl:param name='font-family' select="''"/>
    <xsl:choose>
      <!-- Sans-Serif -->
      <xsl:when test="contains($font-family,'sans','http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive')"><xsl:value-of select="$SansSerifFont"/></xsl:when>
      <xsl:when test="contains($font-family,'helvetic','http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive')"><xsl:value-of select="$SansSerifFont"/></xsl:when>
      <xsl:when test="contains($font-family,'arial','http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive')"><xsl:value-of select="$SansSerifFont"/></xsl:when>

      <!-- Serif -->
      <xsl:when test="contains($font-family,'serif','http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive')"><xsl:value-of select="$SerifFont"/></xsl:when>
      <xsl:when test="contains($font-family,'times','http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive')"><xsl:value-of select="$SerifFont"/></xsl:when>

      <!-- Monospace -->
      <xsl:when test="contains($font-family,'mono','http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive')"><xsl:value-of select="$MonospaceFont"/></xsl:when>
      <xsl:when test="contains($font-family,'courier','http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive')"><xsl:value-of select="$MonospaceFont"/></xsl:when>
      
      <xsl:otherwise><xsl:value-of select="$DefaultFont"/></xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <!-- TGH Append the mapped font-family onto the passed in font value -->
  <xsl:template name="append-font-family">
    <xsl:param name='font' select="''"/>
    <xsl:value-of select="$font"/>,<xsl:call-template name="map-font-family"><xsl:with-param name="font-family" select="$font"></xsl:with-param></xsl:call-template>
  </xsl:template>
  
  <xsl:template match="html:b">
    <fo:inline xsl:use-attribute-sets="b">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:strong">
    <fo:inline xsl:use-attribute-sets="strong">
      <xsl:choose>
        <xsl:when test="@size='1'"></xsl:when>
      </xsl:choose>
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:strong//html:em | html:em//html:strong">
    <fo:inline xsl:use-attribute-sets="strong-em">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:i">
    <fo:inline xsl:use-attribute-sets="i">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:cite">
    <fo:inline xsl:use-attribute-sets="cite">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:em">
    <fo:inline xsl:use-attribute-sets="em">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:var">
    <fo:inline xsl:use-attribute-sets="var">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:dfn">
    <fo:inline xsl:use-attribute-sets="dfn">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:tt">
    <fo:inline xsl:use-attribute-sets="tt">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:code">
    <fo:inline xsl:use-attribute-sets="code">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:kbd">
    <fo:inline xsl:use-attribute-sets="kbd">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:samp">
    <fo:inline xsl:use-attribute-sets="samp">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:big">
    <fo:inline xsl:use-attribute-sets="big">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:small">
    <fo:inline xsl:use-attribute-sets="small">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:sub">
    <fo:inline xsl:use-attribute-sets="sub">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:sup">
    <fo:inline xsl:use-attribute-sets="sup">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:s">
    <fo:inline xsl:use-attribute-sets="s">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:strike">
    <fo:inline xsl:use-attribute-sets="strike">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:del">
    <fo:inline xsl:use-attribute-sets="del">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:u">
    <fo:inline xsl:use-attribute-sets="u">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:ins">
    <fo:inline xsl:use-attribute-sets="ins">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:abbr">
    <fo:inline xsl:use-attribute-sets="abbr">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:acronym">
    <fo:inline xsl:use-attribute-sets="acronym">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:span">
    <fo:inline>
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:span[@dir]">
    <fo:bidi-override direction="{fn:lower-case(fn:normalize-space(@dir))}" unicode-bidi="embed">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:bidi-override>
  </xsl:template>

  <xsl:template match="html:span[@style and contains(@style, 'writing-mode')]">
    <fo:inline-container alignment-baseline="central"
                         text-indent="0pt"
                         last-line-end-indent="0pt"
                         start-indent="0pt"
                         end-indent="0pt"
                         text-align="center"
                         text-align-last="center">
      <xsl:call-template name="process-common-attributes"/>
      <fo:block wrap-option="no-wrap" line-height="1">
        <xsl:apply-templates/>
      </fo:block>
    </fo:inline-container>
  </xsl:template>

  <xsl:template match="html:bdo">
    <fo:bidi-override direction="{fn:lower-case(fn:normalize-space(@dir))}" unicode-bidi="bidi-override">
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:bidi-override>
  </xsl:template>

  <xsl:template match="html:br">
    <fo:block><xsl:call-template name="process-common-attributes"/><xsl:text>&#xA;</xsl:text></fo:block><!-- Using &nbsp; causes XEP to choke.  Might want to check how this deals with multiple <br>s in a row -->
  </xsl:template>

  <xsl:template match="html:q">
    <fo:inline xsl:use-attribute-sets="q">
      <xsl:call-template name="process-common-attributes"/>
      <xsl:choose>
        <xsl:when test="lang('ja')">
          <xsl:text>「</xsl:text>
          <xsl:apply-templates/>
          <xsl:text>」</xsl:text>
        </xsl:when>
        <xsl:otherwise>
          <!-- lang('en') -->
          <xsl:text>“</xsl:text>
          <xsl:apply-templates/>
          <xsl:text>”</xsl:text>
          <!-- TODO: other languages ...-->
        </xsl:otherwise>
      </xsl:choose>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:q//html:q">
    <fo:inline xsl:use-attribute-sets="q-nested">
      <xsl:call-template name="process-common-attributes"/>
      <xsl:choose>
        <xsl:when test="lang('ja')">
          <xsl:text>『</xsl:text>
          <xsl:apply-templates/>
          <xsl:text>』</xsl:text>
        </xsl:when>
        <xsl:otherwise>
          <!-- lang('en') -->
          <xsl:text>‘</xsl:text>
          <xsl:apply-templates/>
          <xsl:text>’</xsl:text>
        </xsl:otherwise>
      </xsl:choose>
    </fo:inline>
  </xsl:template>

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Image
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:template match="html:img">
    <xsl:choose>
      <xsl:when test="fn:lower-case(normalize-space($load-external-images))='true' or fn:starts-with(fn:lower-case(normalize-space(@src)),'cid:')">
        <fo:external-graphic xsl:use-attribute-sets="img">
          <xsl:call-template name="process-img"/>
        </fo:external-graphic>
      </xsl:when>
      <xsl:otherwise>
        <fo:inline xsl:use-attribute-sets="img-omitted">
          <xsl:attribute name="fox:alt-text">Image was not loaded</xsl:attribute>
          <xsl:text> X </xsl:text>
        </fo:inline>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="html:img[ancestor::html:a/@href]">
    <xsl:choose>
      <xsl:when test="fn:lower-case(normalize-space($load-external-images))='true' or fn:starts-with(fn:lower-case(normalize-space(@src)),'cid:')">
        <fo:external-graphic xsl:use-attribute-sets="img-link">
          <xsl:call-template name="process-img"/>
        </fo:external-graphic>
      </xsl:when>
      <xsl:otherwise>
        <fo:inline xsl:use-attribute-sets="img-omitted">
          <xsl:attribute name="fox:alt-text">Image was not loaded</xsl:attribute>
          <xsl:text> X </xsl:text>
        </fo:inline>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="process-img">
    <xsl:variable name="InlineImgSingleBody" select="ancestor::eaxs:Message[1]//eaxs:SingleBody[eaxs:ContentId = fn:substring(normalize-space(current()/@src),5)]"/>
    
    <xsl:choose>
      <xsl:when test="$InlineImgSingleBody">
        <xsl:attribute name="src">
          <xsl:call-template name="GetURLForAttachedContent">
            <xsl:with-param name="inline" select="$InlineImgSingleBody"/>
          </xsl:call-template>
        </xsl:attribute>
        <xsl:if test="$InlineImgSingleBody/eaxs:ContentType">
          <xsl:attribute name="content-type">content-type:<xsl:value-of select="fn:lower-case(normalize-space($InlineImgSingleBody/eaxs:ContentType))"/></xsl:attribute>
        </xsl:if>
        <xsl:call-template name="GetImgAltAttr">
          <xsl:with-param name="inline" select="$InlineImgSingleBody"/>
          <xsl:with-param name="default" select="@alt"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:attribute name="src">
          <xsl:text>url('</xsl:text>
          <xsl:value-of select="@src"/>
          <xsl:text>')</xsl:text>   
        </xsl:attribute>
        <xsl:call-template name="GetImgAltAttr">
          <xsl:with-param name="preferred" select="@alt"/>
        </xsl:call-template>
      </xsl:otherwise>
    </xsl:choose>
    
    <xsl:if test="not(@style) or (@style and not(my:StyleContainsProperty(@style,'overflow')) and not(my:StyleContainsProperty(@style,'overflow-x')) and not(my:StyleContainsProperty(@style,'overflow-y')))">
      <xsl:attribute name="overflow">error-if-overflow</xsl:attribute>
    </xsl:if>
    
    <xsl:choose>
      <xsl:when test="@width">
        <xsl:choose>
          <xsl:when test="contains(@width, '%') and (not(@style) or (@style and not(my:StyleContainsProperty(@style,'width'))))">
            <xsl:attribute name="width">
              <xsl:value-of select="@width"/>
            </xsl:attribute>
            <xsl:attribute name="content-width">scale-to-fit</xsl:attribute>
          </xsl:when>
          <xsl:otherwise>
            <xsl:attribute name="content-width">
              <xsl:value-of select="@width"/>px</xsl:attribute>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:when test="$InlineImgSingleBody/*/eaxs:ImageProperties and (not(@style) or (@style and not(my:StyleContainsProperty(@style,'width'))))">
        <xsl:attribute name="width">
          <xsl:value-of select="my:ScaleToBodyWidth(fn:concat($InlineImgSingleBody/*/eaxs:ImageProperties/eaxs:Width,'px'))"/>
        </xsl:attribute>        
      </xsl:when>
    </xsl:choose>
    
    <xsl:choose>
      <xsl:when test="@height">
        <xsl:choose>
          <xsl:when test="contains(@height, '%') and (not(@style) or (@style and not(my:StyleContainsProperty(@style,'height'))))">
            <xsl:attribute name="height">
              <xsl:value-of select="@height"/>
            </xsl:attribute>
            <xsl:attribute name="content-height">scale-to-fit</xsl:attribute>
          </xsl:when>
          <xsl:otherwise>
            <xsl:attribute name="content-height">
              <xsl:value-of select="@height"/>px</xsl:attribute>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:when test="$InlineImgSingleBody/*/eaxs:ImageProperties and (not(@style) or (@style and not(my:StyleContainsProperty(@style,'height'))))">
        <xsl:attribute name="height">
          <xsl:value-of select="my:ScaleToBodyHeight(fn:concat($InlineImgSingleBody/*/eaxs:ImageProperties/eaxs:Height,'px'))"/>
        </xsl:attribute>        
      </xsl:when>
      <xsl:when test="fn:lower-case(fn:normalize-space($fo-processor))='xep'">
        <!-- XEP crashes if a tall image exceeds the page size, so set the height to a little less than that -->
        <xsl:attribute name="height"><xsl:value-of select="my:PageBodyHeightPts()"/>pt</xsl:attribute>												
      </xsl:when>												        
    </xsl:choose>
    
    <xsl:if test="@border">
      <xsl:attribute name="border">
        <xsl:value-of select="@border"/>px solid</xsl:attribute>
    </xsl:if>
    <xsl:call-template name="process-common-attributes"/>
  </xsl:template>

  <xsl:template match="html:object">
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="html:param"/>
  <xsl:template match="html:map"/>
  <xsl:template match="html:area"/>
  <xsl:template match="html:label"/>
  <xsl:template match="html:input"/>
  <xsl:template match="html:select"/>
  <xsl:template match="html:optgroup"/>
  <xsl:template match="html:option"/>
  <xsl:template match="html:textarea"/>
  <xsl:template match="html:legend"/>
  <xsl:template match="html:button"/>

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Link
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:template match="html:a">
    <fo:inline>
      <xsl:call-template name="process-common-attributes-and-children"/>
    </fo:inline>
  </xsl:template>

  <xsl:template match="html:a[@href]">
    <fo:basic-link xsl:use-attribute-sets="a-link">
      <xsl:call-template name="process-a-link"/>
    </fo:basic-link>
  </xsl:template>

  <xsl:template name="process-a-link">
    <xsl:call-template name="process-common-attributes"/>
    <xsl:choose>
      <xsl:when test="starts-with(@href,'#')">
        <xsl:attribute name="internal-destination">
          <xsl:value-of select="substring-after(@href,'#')"/>
        </xsl:attribute>
      </xsl:when>
      <xsl:otherwise>
        <xsl:attribute name="external-destination">
          <xsl:text>url('</xsl:text>
          <xsl:value-of select="@href"/>
          <xsl:text>')</xsl:text>
        </xsl:attribute>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:if test="@title">
      <xsl:choose>
        <xsl:when test="not(fn:starts-with(fn:lower-case(normalize-space(@title)),'http'))">
          <xsl:call-template name="tag-element">
            <xsl:with-param name="tag" select="@title"/>
          </xsl:call-template>
        </xsl:when>
        <xsl:otherwise>
          <xsl:message>HTML link title attribute title="<xsl:value-of select="@title"/>" is a URI; it is being ignored.</xsl:message>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
    <xsl:apply-templates/>
  </xsl:template>

  <!--=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
       Ruby
  =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-->

  <xsl:template match="html:ruby">
    <fo:inline-container alignment-baseline="central"
                         block-progression-dimension="1em"
                         text-indent="0pt"
                         last-line-end-indent="0pt"
                         start-indent="0pt"
                         end-indent="0pt"
                         text-align="center"
                         text-align-last="center">
      <xsl:call-template name="process-common-attributes"/>
      <fo:block font-size="50%"
                wrap-option="no-wrap"
                line-height="1"
                space-before.conditionality="retain"
                space-before="-1.1em"
                space-after="0.1em"
                role="html:rt"><!-- TODO: This role probably isn't needed -->
        <xsl:for-each select="html:rt | html:rtc[1]/html:rt">
          <xsl:call-template name="process-common-attributes"/>
          <xsl:apply-templates/>
        </xsl:for-each>
      </fo:block>
      <fo:block wrap-option="no-wrap" line-height="1" role="html:rb"> <!-- TODO: This role probably isn't needed -->
        <xsl:for-each select="html:rb | html:rbc[1]/html:rb">
          <xsl:call-template name="process-common-attributes"/>
          <xsl:apply-templates/>
        </xsl:for-each>
      </fo:block>
      <xsl:if test="html:rtc[2]/html:rt">
        <fo:block font-size="50%"
                  wrap-option="no-wrap"
                  line-height="1"
                  space-before="0.1em"
                  space-after.conditionality="retain"
                  space-after="-1.1em"
                  role="html:rt"> <!-- TODO: This role probably isn't needed -->
          <xsl:for-each select="html:rt | html:rtc[2]/html:rt">
            <xsl:call-template name="process-common-attributes"/>
            <xsl:apply-templates/>
          </xsl:for-each>
        </fo:block>
      </xsl:if>
    </fo:inline-container>
  </xsl:template>
</xsl:stylesheet>
