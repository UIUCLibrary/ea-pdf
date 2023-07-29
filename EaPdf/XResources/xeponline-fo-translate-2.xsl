<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:fo="http://www.w3.org/1999/XSL/Format" xmlns:svg="http://www.w3.org/2000/svg"
  xmlns:rx="http://www.renderx.com/XSL/Extensions" version="1.0">
  <!-- Licensing Information
This program is free software; you can redistribute it and/or modify it under the terms of the 
GNU Affero General Public License version 3 as published by the Free Software Foundation with the 
addition of the following permission added to Section 15 as permitted in Section 7(a): 

FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY RENDERX INC, RENDERX DISCLAIMS 
THE WARRANTY OF NON INFRINGEMENT OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even 
the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General 
Public License for more details from the following URL: http://www.gnu.org/licenses/agpl.html
    -->

	<!-- From here:  http://xep.cloudformatter.com/doc/XSL/xeponline-fo-translate-2.xsl -->

  <!-- This sets default margins here although they are set in Javascript
       48px corresponds to 0.5in after the 1.25 factor to scale Web Browser
       Pixels to RenderX pixels -->
  <xsl:param name="def-page-page-width" select="'816px'"/>
  <xsl:param name="def-page-page-height" select="'1056px'"/>
  <xsl:param name="def-page-margin-top" select="'48px'"/>
  <xsl:param name="def-page-margin-bottom" select="'48px'"/>
  <xsl:param name="def-page-margin-left" select="'48px'"/>
  <xsl:param name="def-page-margin-right" select="'48px'"/>


  <xsl:variable name="default.cb-select">✖</xsl:variable>
  <xsl:variable name="default.cb-noselect">&#x274f;</xsl:variable>
  <xsl:variable name="default.ob-select">✔</xsl:variable>
  <xsl:variable name="default.ob-incorrect">✖</xsl:variable>
  <xsl:variable name="default.ob-noselect">&#x274f;</xsl:variable>

  <xsl:variable name="base" select="//div[@class = 'xeponline-document'][1]/@base"/>
  <xsl:variable name="create-bookmarks"
    select="//pdfproperties/pdfpreferences/pref[@name = 'bookmarks']/@value"/>

  <!-- Document -->
  <xsl:template match="//div[@class = 'xeponline-document'][1]">
    <xsl:processing-instruction name="xep-pdf-page-scaling">1.25</xsl:processing-instruction>
    <fo:root font-family="Times New Roman" font-size="14px" id="xeponline-document">
      <xsl:attribute name="xml:base">
        <xsl:value-of select="$base"/>
      </xsl:attribute>
      <!-- Meta Data -->
      <xsl:call-template name="ProcessProperties"/>
      <fo:layout-master-set>
        <xsl:apply-templates select="div[@class = 'xeponline-container']" mode="layout"/>
      </fo:layout-master-set>
      <xsl:if test="$create-bookmarks = 'true'">
        <xsl:call-template name="generate-bookmarks"/>
      </xsl:if>
      <xsl:apply-templates select="div[@class = 'xeponline-container']" mode="sequence"/>
    </fo:root>
  </xsl:template>
  <!-- Processing Properties -->
  <xsl:template name="ProcessProperties">
    <!-- Processing Instructions -->
    <xsl:for-each select="//pdfproperties/pdfpreferences/pref">
      <xsl:if test="not(@name = 'bookmarks')">
        <xsl:processing-instruction name="{concat('xep-pdf-',@name)}"><xsl:value-of select="@value"/></xsl:processing-instruction>
      </xsl:if>
    </xsl:for-each>
    <rx:meta-info>
      <xsl:choose>
        <xsl:when test="//pdfproperties/metainfo/prop">
          <xsl:for-each select="//pdfproperties/metainfo/prop">
            <rx:meta-field name="{@name}" value="{normalize-space(@value)}"/>
          </xsl:for-each>
        </xsl:when>
        <xsl:otherwise>
          <rx:meta-field name="author" value="Xportability LLC"/>
          <rx:meta-field name="title" value="Xportability Cloudformatter"/>
          <rx:meta-field name="subject" value="A Cloudformatter Document"/>
          <rx:meta-field name="creator" value="css2pdf@cloudformatter.com"/>
        </xsl:otherwise>
      </xsl:choose>
    </rx:meta-info>
  </xsl:template>
  <!-- Section Layout -->
  <xsl:template match="div[@class = 'xeponline-container']" mode="layout">
    <xsl:variable name="section" select="concat('section_', position())"/>
    <xsl:variable name="style-margin">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="div[1]/@style"/>
        <xsl:with-param name="style" select="'margin:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:variable name="style-margin-top">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="div[1]/@style"/>
        <xsl:with-param name="style" select="'margin-top:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:variable name="style-margin-bottom">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="div[1]/@style"/>
        <xsl:with-param name="style" select="'margin-bottom:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:variable name="style-margin-left">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="div[1]/@style"/>
        <xsl:with-param name="style" select="'margin-left:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:variable name="style-margin-right">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="div[1]/@style"/>
        <xsl:with-param name="style" select="'margin-right:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:variable name="margin-top">
      <xsl:choose>
        <xsl:when test="string-length($style-margin-top) > 0">
          <xsl:value-of select="$style-margin-top"/>
        </xsl:when>
        <xsl:when test="string-length($style-margin) > 0">
          <xsl:value-of select="$style-margin"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$def-page-margin-top"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:variable name="margin-bottom">
      <xsl:choose>
        <xsl:when test="string-length($style-margin-bottom) > 0">
          <xsl:value-of select="$style-margin-bottom"/>
        </xsl:when>
        <xsl:when test="string-length($style-margin) > 0">
          <xsl:value-of select="$style-margin"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$def-page-margin-bottom"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:variable name="margin-left">
      <xsl:choose>
        <xsl:when test="string-length($style-margin-left) > 0">
          <xsl:value-of select="$style-margin-left"/>
        </xsl:when>
        <xsl:when test="string-length($style-margin) > 0">
          <xsl:value-of select="$style-margin"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$def-page-margin-left"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:variable name="margin-right">
      <xsl:choose>
        <xsl:when test="string-length($style-margin-right) > 0">
          <xsl:value-of select="$style-margin-right"/>
        </xsl:when>
        <xsl:when test="string-length($style-margin) > 0">
          <xsl:value-of select="$style-margin"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$def-page-margin-right"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <fo:simple-page-master>
      <xsl:attribute name="master-name">
        <xsl:value-of select="$section"/>
      </xsl:attribute>
      <xsl:variable name="page-width">
        <xsl:call-template name="extractCSSStyle">
          <xsl:with-param name="css" select="@style"/>
          <xsl:with-param name="style" select="'width:'"/>
        </xsl:call-template>
      </xsl:variable>
      <xsl:attribute name="page-width">
        <xsl:choose>
          <xsl:when test="string-length($page-width) > 0">
            <xsl:value-of select="$page-width"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="$def-page-page-width"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <xsl:variable name="page-height">
        <xsl:call-template name="extractCSSStyle">
          <xsl:with-param name="css" select="@style"/>
          <xsl:with-param name="style" select="'height:'"/>
        </xsl:call-template>
      </xsl:variable>
      <xsl:attribute name="page-height">
        <xsl:choose>
          <xsl:when test="string-length($page-height) > 0">
            <xsl:value-of select="$page-height"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="$def-page-page-height"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <fo:region-body>
        <xsl:attribute name="margin-left">
          <xsl:value-of select="$margin-left"/>
        </xsl:attribute>
        <xsl:attribute name="margin-right">
          <xsl:value-of select="$margin-right"/>
        </xsl:attribute>
        <xsl:attribute name="margin-top">
          <xsl:value-of select="$margin-top"/>
        </xsl:attribute>
        <xsl:attribute name="margin-bottom">
          <xsl:value-of select="$margin-bottom"/>
        </xsl:attribute>
      </fo:region-body>
      <fo:region-before>
        <xsl:attribute name="extent">
          <xsl:value-of select="$page-height"/>
        </xsl:attribute>
        <xsl:if test="descendant::header[1]/@background-color">
          <xsl:attribute name="background-color">
            <xsl:value-of select="descendant::header[1]/@background-color"/>
          </xsl:attribute>
        </xsl:if>
        <xsl:if test="descendant::header[1]/@page-background">
          <xsl:attribute name="background-image">
            <xsl:value-of select="descendant::header[1]/@page-background"/>
          </xsl:attribute>
        </xsl:if>
        <xsl:if test="descendant::header[1]/@border">
          <xsl:attribute name="border">
            <xsl:value-of select="descendant::header[1]/@border"/>
          </xsl:attribute>
        </xsl:if>
        <xsl:if test="descendant::header[1]/@border-margin">
          <xsl:attribute name="margin">
            <xsl:value-of select="descendant::header[1]/@border-margin"/>
          </xsl:attribute>
          <xsl:attribute name="padding">
            <xsl:value-of select="concat('-1 * ', descendant::header[1]/@border-margin)"/>
          </xsl:attribute>
        </xsl:if>
      </fo:region-before>
      <fo:region-after>
        <xsl:attribute name="extent">
          <xsl:value-of select="$margin-bottom"/>
        </xsl:attribute>
      </fo:region-after>
    </fo:simple-page-master>
  </xsl:template>
  <!-- Section Content -->
  <xsl:template match="div[@class = 'xeponline-container']" mode="sequence">
    <xsl:variable name="section" select="concat('section_', position())"/>
    <xsl:variable name="style-margin">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="div[1]/@style"/>
        <xsl:with-param name="style" select="'margin:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:variable name="style-margin-left">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="div[1]/@style"/>
        <xsl:with-param name="style" select="'margin-left:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:variable name="style-margin-right">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="div[1]/@style"/>
        <xsl:with-param name="style" select="'margin-right:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:variable name="margin-left">
      <xsl:choose>
        <xsl:when test="string-length($style-margin-left) > 0">
          <xsl:value-of select="$style-margin-left"/>
        </xsl:when>
        <xsl:when test="string-length($style-margin) > 0">
          <xsl:value-of select="$style-margin"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$def-page-margin-left"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:variable name="margin-right">
      <xsl:choose>
        <xsl:when test="string-length($style-margin-right) > 0">
          <xsl:value-of select="$style-margin-right"/>
        </xsl:when>
        <xsl:when test="string-length($style-margin) > 0">
          <xsl:value-of select="$style-margin"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$def-page-margin-right"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <fo:page-sequence master-reference="{$section}">
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <fo:static-content flow-name="xsl-region-before">
        <xsl:apply-templates select="descendant::header[1]" mode="regions">
          <xsl:with-param name="margin-left" select="$margin-left"/>
          <xsl:with-param name="margin-right" select="$margin-right"/>
        </xsl:apply-templates>
      </fo:static-content>
      <fo:static-content flow-name="xsl-region-after">
        <xsl:apply-templates select="descendant::footer[1]" mode="regions">
          <xsl:with-param name="margin-left" select="$margin-left"/>
          <xsl:with-param name="margin-right" select="$margin-right"/>
        </xsl:apply-templates>
      </fo:static-content>
      <fo:static-content flow-name="xsl-footnote-separator">
        <fo:block>
          <fo:leader leader-pattern="rule" leader-length="100%" rule-thickness="0.5pt"
            rule-style="solid" color="black"/>
        </fo:block>
      </fo:static-content>
      <fo:flow flow-name="xsl-region-body">
        <xsl:apply-templates select="child::div[1]/*"/>
      </fo:flow>
    </fo:page-sequence>
  </xsl:template>
  <!-- Header/Footer -->
  <xsl:template match="header | footer" mode="regions">
    <xsl:param name="margin-left"/>
    <xsl:param name="margin-right"/>
    <fo:block margin-left="{$margin-left}" margin-right="{$margin-right}">
      <xsl:variable name="margin-top">
        <xsl:call-template name="extractCSSStyle">
          <xsl:with-param name="css" select="@style"/>
          <xsl:with-param name="style" select="'margin-top:'"/>
        </xsl:call-template>
      </xsl:variable>
      <xsl:if test="string-length($margin-top) > 0">
        <xsl:attribute name="margin-top">
          <xsl:value-of select="$margin-top"/>
        </xsl:attribute>
      </xsl:if>
      <xsl:apply-templates select="node()"/>
    </fo:block>
  </xsl:template>
  <!-- Flow Section -->
  <xsl:template match="div[@column-count]" priority="1">
    <rx:flow-section column-count="{@column-count}">
      <xsl:call-template name="blockcontent"/>
    </rx:flow-section>
  </xsl:template>
  <xsl:template match="div[contains(@style, 'column-count')]" priority="1">
    <rx:flow-section>
      <xsl:attribute name="column-count">
        <!-- Hack for now as it could any one of three attributes -->
        <xsl:value-of
          select="normalize-space(substring-before(substring-after(@style, 'column-count:'), ';'))"
        />
      </xsl:attribute>
      <xsl:call-template name="blockcontent"/>
    </rx:flow-section>
  </xsl:template>
  <!-- Block level elements -->
  <xsl:template
    match="section | article | address | main | div | h1 | h2 | h3 | h4 | h5 | h6 | h7 | h8 | h9 | p | blockquote | figure | figcaption">
    <xsl:call-template name="blockcontent"/>
  </xsl:template>
  <xsl:template name="blockcontent">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <xsl:variable name="floatType">
        <xsl:call-template name="floatType"/>
      </xsl:variable>
      <xsl:choose>
        <xsl:when test="$floatType = 'none'">
          <xsl:variable name="isPositioned">
            <xsl:call-template name="isPositioned"/>
          </xsl:variable>
          <xsl:choose>
            <xsl:when test="not($isPositioned = '')">
              <fo:block-container>
                <xsl:call-template name="processAttr">
                  <xsl:with-param name="elem" select="."/>
                  <xsl:with-param name="type" select="name()"/>
                </xsl:call-template>
                <!--                <xsl:attribute name="position">
                  <xsl:value-of select="$isPositioned"/>
                </xsl:attribute>
                <xsl:variable name="top">
                  <xsl:call-template name="extractCSSStyle">
                    <xsl:with-param name="css" select="@style"/>
                    <xsl:with-param name="style" select="'top:'"/>
                  </xsl:call-template>
                </xsl:variable>
                <xsl:if test="not($top = '')">
                  <xsl:attribute name="top">
                    <xsl:value-of select="$top"/>
                  </xsl:attribute>
                </xsl:if>
                <xsl:variable name="bottom">
                  <xsl:call-template name="extractCSSStyle">
                    <xsl:with-param name="css" select="@style"/>
                    <xsl:with-param name="style" select="'bottom:'"/>
                  </xsl:call-template>
                </xsl:variable>
                <xsl:if test="not($bottom = '')">
                  <xsl:attribute name="bottom">
                    <xsl:value-of select="$bottom"/>
                  </xsl:attribute>
                </xsl:if>
                <xsl:variable name="left">
                  <xsl:call-template name="extractCSSStyle">
                    <xsl:with-param name="css" select="@style"/>
                    <xsl:with-param name="style" select="'left:'"/>
                  </xsl:call-template>
                </xsl:variable>
                <xsl:if test="not($left = '')">
                  <xsl:attribute name="left">
                    <xsl:value-of select="$left"/>
                  </xsl:attribute>
                </xsl:if>
                <xsl:variable name="right">
                  <xsl:call-template name="extractCSSStyle">
                    <xsl:with-param name="css" select="@style"/>
                    <xsl:with-param name="style" select="'right:'"/>
                  </xsl:call-template>
                </xsl:variable>
                <xsl:if test="not($right = '')">
                  <xsl:attribute name="right">
                    <xsl:value-of select="$right"/>
                  </xsl:attribute>
                </xsl:if>
                <xsl:variable name="width">
                  <xsl:call-template name="extractCSSStyle">
                    <xsl:with-param name="css" select="@style"/>
                    <xsl:with-param name="style" select="'width:'"/>
                  </xsl:call-template>
                </xsl:variable>
                <xsl:if test="not($width = '')">
                  <xsl:attribute name="width">
                    <xsl:value-of select="$width"/>
                  </xsl:attribute>
                </xsl:if>
                <xsl:variable name="height">
                  <xsl:call-template name="extractCSSStyle">
                    <xsl:with-param name="css" select="@style"/>
                    <xsl:with-param name="style" select="'height:'"/>
                  </xsl:call-template>
                </xsl:variable>
                <xsl:if test="not($height = '')">
                  <xsl:attribute name="height">
                    <xsl:value-of select="$height"/>
                  </xsl:attribute>
                </xsl:if>-->
                <fo:block margin="0">
                  <xsl:attribute name="id">
                    <xsl:value-of select="generate-id()"/>
                  </xsl:attribute>
                  <!--<xsl:call-template name="processAttr">
                    <xsl:with-param name="elem" select="."/>
                    <xsl:with-param name="type" select="name()"/>
                  </xsl:call-template>-->
                  <xsl:apply-templates select="node()"/>
                </fo:block>
              </fo:block-container>
            </xsl:when>
            <xsl:otherwise>
              <!-- KB: Testing here. Apply clear="both" automatically at the start as HTML is default this -->
              <fo:block margin="0" clear="both">
                <xsl:attribute name="id">
                  <xsl:value-of select="generate-id()"/>
                </xsl:attribute>
                <xsl:call-template name="processAttr">
                  <xsl:with-param name="elem" select="."/>
                  <xsl:with-param name="type" select="name()"/>
                </xsl:call-template>
                <xsl:apply-templates select="node()"/>
              </fo:block>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:when>
        <xsl:otherwise>
          <xsl:variable name="ignoreHeight">
            <xsl:choose>
              <xsl:when test="contains(@class, 'ignoreHeight')">
                <xsl:text>true</xsl:text>
              </xsl:when>
              <xsl:otherwise>
                <xsl:text>false</xsl:text>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <fo:float float="{$floatType}" clear="none">
            <xsl:variable name="clear">
              <xsl:call-template name="extractCSSStyle">
                <xsl:with-param name="css" select="@style"/>
                <xsl:with-param name="style" select="'clear:'"/>
              </xsl:call-template>
            </xsl:variable>
            <xsl:if test="string-length($clear) > 0">
              <xsl:attribute name="clear">
                <xsl:value-of select="$clear"/>
              </xsl:attribute>
            </xsl:if>
            <fo:block-container margin="0">
              <xsl:call-template name="processAttr">
                <xsl:with-param name="elem" select="."/>
                <xsl:with-param name="type" select="name()"/>
                <xsl:with-param name="float" select="'true'"/>
                <xsl:with-param name="ignoreHeight" select="$ignoreHeight"/>
              </xsl:call-template>
              <fo:block>
                <xsl:apply-templates/>
              </fo:block>
            </fo:block-container>
          </fo:float>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
  </xsl:template>
  <xsl:template match="pre">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:block white-space="pre" linefeed-treatment="preserve" white-space-collapse="false"
        margin="0">
        <xsl:call-template name="processAttr">
          <xsl:with-param name="elem" select="."/>
          <xsl:with-param name="type" select="name()"/>
        </xsl:call-template>
        <xsl:apply-templates select="node()"/>
      </fo:block>
    </xsl:if>
  </xsl:template>
  <xsl:template match="br">
    <fo:block>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <fo:leader/>
    </fo:block>
  </xsl:template>
  <xsl:template match="hr">
    <fo:block>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <xsl:attribute name="font-size">0pt</xsl:attribute>
      <xsl:attribute name="line-height">0pt</xsl:attribute>
      <xsl:attribute name="border-top">none</xsl:attribute>
      <xsl:attribute name="border-left">none</xsl:attribute>
      <xsl:attribute name="border-right">none</xsl:attribute>
      <xsl:attribute name="border-bottom-width">1px</xsl:attribute>
      <fo:leader/>
    </fo:block>
  </xsl:template>
  <!--Inlines -->
  <xsl:template
    match="span | i | b | sup | sub | font | code | em | small | strike | strong | u | dfn | abbr | code | var | samp | kbd | mark">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <xsl:variable name="isBlock">
        <xsl:call-template name="isBlock"/>
      </xsl:variable>
      <xsl:choose>
        <xsl:when test="$isBlock = 'true'">
          <fo:block>
            <xsl:call-template name="processAttr">
              <xsl:with-param name="elem" select="."/>
              <xsl:with-param name="type" select="name()"/>
            </xsl:call-template>
            <xsl:if test="name() = 'u'">
              <xsl:attribute name="text-decoration">underline</xsl:attribute>
            </xsl:if>
            <xsl:if test="name() = 'sup'">
              <xsl:attribute name="baseline-shift">0.5em</xsl:attribute>
            </xsl:if>
            <xsl:if test="name() = 'sub'">
              <xsl:attribute name="baseline-shift">-0.5em</xsl:attribute>
            </xsl:if>
            <xsl:if test="name() = 'strike'">
              <xsl:attribute name="text-decoration">line-through</xsl:attribute>
            </xsl:if>
            <xsl:apply-templates select="node()"/>
          </fo:block>
        </xsl:when>
        <xsl:otherwise>
          <fo:inline>
            <xsl:call-template name="processAttr">
              <xsl:with-param name="elem" select="."/>
              <xsl:with-param name="type" select="name()"/>
            </xsl:call-template>
            <xsl:if test="name() = 'u'">
              <xsl:attribute name="text-decoration">underline</xsl:attribute>
            </xsl:if>
            <xsl:if test="name() = 'sup'">
              <xsl:attribute name="baseline-shift">0.5em</xsl:attribute>
            </xsl:if>
            <xsl:if test="name() = 'sub'">
              <xsl:attribute name="baseline-shift">-0.5em</xsl:attribute>
            </xsl:if>
            <xsl:if test="name() = 'strike'">
              <xsl:attribute name="text-decoration">line-through</xsl:attribute>
            </xsl:if>
            <xsl:apply-templates select="node()"/>
          </fo:inline>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
  </xsl:template>
  <xsl:template match="a">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:basic-link>
        <xsl:call-template name="processAttr">
          <xsl:with-param name="elem" select="."/>
          <xsl:with-param name="type" select="name()"/>
        </xsl:call-template>
        <xsl:apply-templates select="node()"/>
      </fo:basic-link>
    </xsl:if>
  </xsl:template>
  <xsl:template match="a/@href">
    <xsl:choose>
      <xsl:when test="starts-with(., '#')">
        <xsl:attribute name="internal-destination">
          <xsl:value-of select="translate(., '#', '')"/>
        </xsl:attribute>
      </xsl:when>
      <xsl:otherwise>
        <xsl:attribute name="external-destination">
          <xsl:choose>
            <xsl:when test="starts-with(., 'url(')">
              <xsl:value-of select="."/>
            </xsl:when>
            <xsl:when test="starts-with(., 'http:') or starts-with(., 'https:')">
              <xsl:text>url('</xsl:text>
              <xsl:value-of select="."/>
              <xsl:text>')</xsl:text>
            </xsl:when>
            <xsl:otherwise>
              <xsl:variable name="locref">
                <xsl:choose>
                  <xsl:when test="starts-with(., '/')">
                    <xsl:value-of select="substring-after(., '/')"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:value-of select="."/>
                  </xsl:otherwise>
                </xsl:choose>
              </xsl:variable>
              <xsl:text>url('</xsl:text>
              <xsl:value-of select="concat($base, $locref)"/>
              <xsl:text>')</xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <xsl:template match="q">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:inline>
        <xsl:call-template name="processAttr">
          <xsl:with-param name="elem" select="."/>
          <xsl:with-param name="type" select="name()"/>
        </xsl:call-template>
        <xsl:text>&quot;</xsl:text>
        <xsl:apply-templates select="node()"/>
        <xsl:text>&quot;</xsl:text>
      </fo:inline>
    </xsl:if>
  </xsl:template>
  <!-- Markers -->
  <xsl:template match="marker">
    <fo:marker>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <xsl:apply-templates/>
    </fo:marker>
  </xsl:template>
  <xsl:template match="retrieve-marker">
    <fo:retrieve-marker>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
    </fo:retrieve-marker>
  </xsl:template>
  <!-- Images -->
  <xsl:template match="img">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:external-graphic>
        <xsl:call-template name="processAttr">
          <xsl:with-param name="elem" select="."/>
          <xsl:with-param name="type" select="name()"/>
        </xsl:call-template>
      </fo:external-graphic>
    </xsl:if>
  </xsl:template>
  <xsl:template match="object">
    <xsl:choose>
      <xsl:when test="@type = 'application/pdf'">
        <fo:external-graphic>
          <xsl:call-template name="processAttr">
            <xsl:with-param name="elem" select="."/>
            <xsl:with-param name="type" select="name()"/>
          </xsl:call-template>
          <xsl:attribute name="src">
            <xsl:value-of select="@data"/>
          </xsl:attribute>
          <xsl:attribute name="content-type">
            <xsl:value-of select="@type"/>
          </xsl:attribute>
        </fo:external-graphic>
      </xsl:when>
    </xsl:choose>
  </xsl:template>
  <xsl:template match="video">
    <rx:media-object embed="true">
      <xsl:attribute name="src">
        <xsl:value-of select="source/@src"/>
      </xsl:attribute>
      <xsl:attribute name="content-type">
        <xsl:value-of select="source/@type"/>
      </xsl:attribute>
      <xsl:attribute name="content-width">
        <xsl:value-of select="concat(@width, 'px')"/>
      </xsl:attribute>
      <xsl:attribute name="content-height">
        <xsl:value-of select="concat(@height, 'px')"/>
      </xsl:attribute>
    </rx:media-object>
  </xsl:template>
  <xsl:template match="svg:svg">
    <fo:instream-foreign-object>
      <svg:svg>
        <xsl:apply-templates select="@* | node()"/>
      </svg:svg>
    </fo:instream-foreign-object>
  </xsl:template>
  <xsl:template match="@gradientUnits">
    <xsl:attribute name="gradientUnits">
      <xsl:text>userSpaceOnUse</xsl:text>
    </xsl:attribute>
  </xsl:template>
  <!-- Tables -->
  <xsl:template match="table">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <xsl:choose>
        <xsl:when test="child::caption">
          <fo:table-and-caption table-layout="fixed" border-width="1px">
            <xsl:apply-templates select="caption"/>
            <fo:table table-layout="fixed" border-width="1px">
              <xsl:call-template name="processAttr">
                <xsl:with-param name="elem" select="."/>
                <xsl:with-param name="type" select="name()"/>
              </xsl:call-template>
              <xsl:apply-templates select="node()[not(name()='caption')]"/>
            </fo:table>
          </fo:table-and-caption>
        </xsl:when>
        <xsl:otherwise>
          <fo:table table-layout="fixed" border-width="1px">
            <xsl:call-template name="processAttr">
              <xsl:with-param name="elem" select="."/>
              <xsl:with-param name="type" select="name()"/>
            </xsl:call-template>
            <xsl:apply-templates select="node()"/>
          </fo:table>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
  </xsl:template>
  <xsl:template match="caption">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:table-caption>
        <xsl:call-template name="processAttr">
          <xsl:with-param name="elem" select="."/>
          <xsl:with-param name="type" select="name()"/>
        </xsl:call-template>
        <fo:block>
          <xsl:apply-templates/>  
        </fo:block>
      </fo:table-caption>
    </xsl:if>
  </xsl:template>
  <xsl:template match="colgroup">
    <xsl:if test="count(col) > 0">
      <xsl:for-each select="col">
        <fo:table-column>
          <xsl:if test="@width">
            <xsl:attribute name="column-width">
              <xsl:value-of select="@width"/>
              <xsl:text>px</xsl:text>
            </xsl:attribute>
          </xsl:if>
        </fo:table-column>
      </xsl:for-each>
    </xsl:if>
  </xsl:template>
  <xsl:template match="tr">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:table-row>
        <xsl:call-template name="processAttr">
          <xsl:with-param name="elem" select="."/>
          <xsl:with-param name="type" select="name()"/>
        </xsl:call-template>
        <xsl:apply-templates/>
      </fo:table-row>
    </xsl:if>
  </xsl:template>
  <xsl:template match="td | th">
    <!-- Trying IE Hack, set default border to no width -->
    <fo:table-cell border-width="1px">
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <fo:block>
        <xsl:apply-templates select="node()"/>
      </fo:block>
    </fo:table-cell>
  </xsl:template>
  <xsl:template match="thead">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:table-header>
        <xsl:call-template name="processAttr">
          <xsl:with-param name="elem" select="."/>
          <xsl:with-param name="type" select="name()"/>
        </xsl:call-template>
        <xsl:apply-templates select="tr"/>
      </fo:table-header>
    </xsl:if>
  </xsl:template>
  <xsl:template match="tfoot">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:table-footer>
        <xsl:call-template name="processAttr">
          <xsl:with-param name="elem" select="."/>
          <xsl:with-param name="type" select="name()"/>
        </xsl:call-template>
        <xsl:apply-templates select="tr"/>
      </fo:table-footer>
    </xsl:if>
  </xsl:template>
  <xsl:template match="tbody">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:table-body>
        <xsl:call-template name="processAttr">
          <xsl:with-param name="elem" select="."/>
          <xsl:with-param name="type" select="name()"/>
        </xsl:call-template>
        <xsl:apply-templates select="tr"/>
      </fo:table-body>
    </xsl:if>
  </xsl:template>
  <xsl:template match="@colspan">
    <xsl:attribute name="number-columns-spanned">
      <xsl:value-of select="."/>
    </xsl:attribute>
  </xsl:template>
  <xsl:template match="@rowspan">
    <xsl:attribute name="number-rows-spanned">
      <xsl:value-of select="."/>
    </xsl:attribute>
  </xsl:template>
  <!-- Lists -->
  <xsl:template match="ul[contains(@style, 'display: inline')]">
    <fo:block>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <xsl:apply-templates select="*" mode="inline"/>
    </fo:block>
  </xsl:template>
  <xsl:template match="ul | ol">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:list-block>
        <xsl:call-template name="processAttr">
          <xsl:with-param name="elem" select="."/>
          <xsl:with-param name="type" select="name()"/>
        </xsl:call-template>
        <xsl:variable name="listStyleType">
          <xsl:call-template name="getListStyle">
            <xsl:with-param name="lists" select="ancestor-or-self::ul | ancestor-or-self::ol"/>
            <xsl:with-param name="pos" select="1"/>
          </xsl:call-template>
        </xsl:variable>
        <xsl:variable name="listStyleImage">
          <xsl:call-template name="getListImage">
            <xsl:with-param name="images"
              select="ancestor-or-self::ul/li[1] | ancestor-or-self::ol/li[1]"/>
            <xsl:with-param name="pos" select="1"/>
          </xsl:call-template>
        </xsl:variable>
        <xsl:apply-templates>
          <xsl:with-param name="listStyleType" select="$listStyleType"/>
          <xsl:with-param name="listStyleImage" select="$listStyleImage"/>
        </xsl:apply-templates>
      </fo:list-block>
    </xsl:if>
  </xsl:template>
  <xsl:template name="getListStyle">
    <xsl:param name="lists"/>
    <xsl:param name="pos"/>
    <xsl:variable name="parentListstyle">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="$lists[number($pos)]/@style"/>
        <xsl:with-param name="style" select="'list-style-type:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:choose>
      <xsl:when test="$parentListstyle != ''">
        <xsl:value-of select="$parentListstyle"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:choose>
          <xsl:when test="number($pos) &lt;= count($lists)">
            <xsl:call-template name="getListStyle">
              <xsl:with-param name="lists" select="$lists"/>
              <xsl:with-param name="pos" select="$pos + 1"/>
            </xsl:call-template>
          </xsl:when>
          <xsl:otherwise>
            <xsl:choose>
              <xsl:when test="name() = 'ul'">
                <xsl:choose>
                  <xsl:when test="count($lists) = 1">
                    <xsl:value-of select="'disk'"/>
                  </xsl:when>
                  <xsl:when test="count($lists) = 2">
                    <xsl:value-of select="'circle'"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:value-of select="'square'"/>
                  </xsl:otherwise>
                </xsl:choose>
              </xsl:when>
              <xsl:otherwise>
                <xsl:value-of select="'decimal'"/>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <xsl:template name="getListImage">
    <xsl:param name="images"/>
    <xsl:param name="pos"/>
    <xsl:variable name="parentListstyle">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="$images[number($pos)]/@style"/>
        <xsl:with-param name="style" select="'list-style-image:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:choose>
      <xsl:when test="$parentListstyle != ''">
        <xsl:value-of select="$parentListstyle"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:choose>
          <xsl:when test="number($pos) &lt;= count($images)">
            <xsl:call-template name="getListImage">
              <xsl:with-param name="images" select="$images"/>
              <xsl:with-param name="pos" select="$pos + 1"/>
            </xsl:call-template>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="''"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <xsl:template match="li" mode="inline">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:inline>
        <xsl:call-template name="processAttr">
          <xsl:with-param name="elem" select="."/>
          <xsl:with-param name="type" select="'inline'"/>
        </xsl:call-template>
        <xsl:apply-templates/>
      </fo:inline>
    </xsl:if>
  </xsl:template>
  <xsl:template match="li">
    <xsl:param name="listStyleType"/>
    <xsl:param name="listStyleImage"/>
    <xsl:choose>
      <xsl:when test="parent::ol or parent::ul">
        <xsl:call-template name="listitem">
          <xsl:with-param name="listStyleType" select="$listStyleType"/>
          <xsl:with-param name="listStyleImage"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <fo:list-block>
          <xsl:call-template name="listitem">
            <xsl:with-param name="listStyleType" select="'disk'"/>
            <xsl:with-param name="listStyleImage"/>
          </xsl:call-template>
        </fo:list-block>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <xsl:template name="listitem">
    <xsl:param name="listStyleType"/>
    <xsl:param name="listStyleImage"/>
    <xsl:variable name="pos" select="count(preceding-sibling::li) + 1"/>
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:list-item>
        <fo:list-item-label end-indent="label-end()">
          <fo:block text-align="right">
            <xsl:choose>
              <xsl:when test="$listStyleImage != ''">
                <fo:inline>
                  <fo:external-graphic src="{$listStyleImage}"/>
                </fo:inline>
              </xsl:when>
              <xsl:when test="$listStyleType = 'disk'">
                <fo:inline baseline-shift="25%" font-size="40%" font-family="ZapfDingbats"
                  >●</fo:inline>
              </xsl:when>
              <xsl:when test="$listStyleType = 'square'">
                <fo:inline baseline-shift="25%" font-size="40%" font-family="ZapfDingbats"
                  >■</fo:inline>
              </xsl:when>
              <xsl:when test="$listStyleType = 'circle'">
                <fo:inline baseline-shift="25%" font-size="40%" font-family="ZapfDingbats"
                  >❍</fo:inline>
              </xsl:when>
              <xsl:when test="$listStyleType = 'decimal'">
                <xsl:number value="$pos" format="1"/>
                <xsl:text>.</xsl:text>
              </xsl:when>
              <xsl:when test="$listStyleType = 'decimal-leading-zero'">
                <xsl:number value="$pos" format="01"/>
                <xsl:text>.</xsl:text>
              </xsl:when>
              <xsl:when test="$listStyleType = 'lower-alpha' or $listStyleType = 'lower-latin'">
                <xsl:number value="$pos" format="a"/>
                <xsl:text>.</xsl:text>
              </xsl:when>
              <xsl:when test="$listStyleType = 'lower-roman'">
                <xsl:number value="$pos" format="i"/>
                <xsl:text>.</xsl:text>
              </xsl:when>
              <xsl:when test="$listStyleType = 'upper-alpha' or $listStyleType = 'upper-latin'">
                <xsl:number value="$pos" format="A"/>
                <xsl:text>.</xsl:text>
              </xsl:when>
              <xsl:when test="$listStyleType = 'upper-roman'">
                <xsl:number value="$pos" format="I"/>
                <xsl:text>.</xsl:text>
              </xsl:when>
              <xsl:when test="$listStyleType = 'none'"/>
              <xsl:otherwise>
                <fo:inline baseline-shift="25%" font-size="40%" font-family="ZapfDingbats"
                  >●</fo:inline>
              </xsl:otherwise>
            </xsl:choose>
          </fo:block>
        </fo:list-item-label>
        <fo:list-item-body start-indent="body-start()">
          <fo:block>
            <xsl:call-template name="processAttr">
              <xsl:with-param name="elem" select="."/>
              <xsl:with-param name="type" select="name()"/>
            </xsl:call-template>
            <xsl:apply-templates/>
          </fo:block>
        </fo:list-item-body>
      </fo:list-item>
    </xsl:if>
  </xsl:template>
  <!-- Form Elements -->
  <xsl:template match="form">
    <fo:block>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <xsl:apply-templates/>
    </fo:block>
  </xsl:template>
  <xsl:template match="input[@type = 'text']">
    <fo:inline>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <rx:pdf-form-field>
        <xsl:call-template name="form_field_attributes"/>
        <rx:pdf-form-field-text text="{@value}"/>
      </rx:pdf-form-field>
      <fo:leader>
        <xsl:attribute name="leader-length">
          <xsl:call-template name="extractCSSStyle">
            <xsl:with-param name="css" select="@style"/>
            <xsl:with-param name="style" select="'width:'"/>
          </xsl:call-template>
        </xsl:attribute>
      </fo:leader>
    </fo:inline>
  </xsl:template>
  <xsl:template match="textarea">
    <!-- A text area cannot be in an inline, it must be in a constrained area so we use a block-container -->
    <fo:block-container>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <rx:pdf-form-field>
        <xsl:call-template name="form_field_attributes"/>
        <xsl:element name="rx:pdf-form-field-text">
          <xsl:attribute name="text">
            <xsl:value-of select="."/>
          </xsl:attribute>
          <xsl:attribute name="multiline">
            <xsl:text>true</xsl:text>
          </xsl:attribute>
        </xsl:element>
      </rx:pdf-form-field>
      <fo:block>
        <fo:leader>
          <xsl:attribute name="leader-length">
            <xsl:call-template name="extractCSSStyle">
              <xsl:with-param name="css" select="@style"/>
              <xsl:with-param name="style" select="'width:'"/>
            </xsl:call-template>
          </xsl:attribute>
        </fo:leader>
      </fo:block>
    </fo:block-container>
  </xsl:template>
  <xsl:template match="input[@type = 'checkbox']">
    <xsl:variable name="isVisible">
      <xsl:call-template name="isVisible"/>
    </xsl:variable>
    <xsl:if test="$isVisible = 'true'">
      <fo:inline>
        <xsl:attribute name="border-width">1pt</xsl:attribute>
        <xsl:attribute name="border-style">solid</xsl:attribute>
        <xsl:attribute name="border-color">silver</xsl:attribute>
        <xsl:attribute name="background-color">lavender</xsl:attribute>
        <xsl:attribute name="font-family">ZapfDingbats</xsl:attribute>
        <xsl:attribute name="font-size">14px</xsl:attribute>
        <fo:inline>
          <xsl:element name="rx:pdf-form-field">
            <xsl:call-template name="form_field_attributes"/>
            <xsl:element name="rx:pdf-form-field-checkbox">
              <rx:pdf-form-field-option text="{$default.cb-select}"/>
              <rx:pdf-form-field-option text="&#x21;"/>
            </xsl:element>
          </xsl:element>
          <fo:leader leader-length="12px"/>
        </fo:inline>
      </fo:inline>
    </xsl:if>
  </xsl:template>
  <xsl:template match="select">
    <fo:inline>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <rx:pdf-form-field>
        <xsl:call-template name="form_field_attributes"/>
        <xsl:element name="rx:pdf-form-field-combobox">
          <xsl:for-each select="option">
            <rx:pdf-form-field-option>
              <xsl:attribute name="text">
                <xsl:value-of select="."/>
              </xsl:attribute>
              <xsl:if test="@selected">
                <xsl:attribute name="initially-selected">true</xsl:attribute>
              </xsl:if>
            </rx:pdf-form-field-option>
          </xsl:for-each>
        </xsl:element>
      </rx:pdf-form-field>
      <fo:inline>
        <fo:leader>
          <xsl:attribute name="leader-length">
            <xsl:call-template name="extractCSSStyle">
              <xsl:with-param name="css" select="@style"/>
              <xsl:with-param name="style" select="'width:'"/>
            </xsl:call-template>
          </xsl:attribute>
        </fo:leader>
      </fo:inline>
    </fo:inline>
  </xsl:template>
  <!-- Common Field-level Javascript Attributes -->
  <xsl:template name="calculate">
    <xsl:if test="@calculate">
      <xsl:attribute name="js-calculate">
        <xsl:value-of select="@calculate"/>
      </xsl:attribute>
    </xsl:if>
  </xsl:template>
  <xsl:template name="validate">
    <xsl:if test="@validate">
      <xsl:attribute name="js-validate">
        <xsl:value-of select="@validate"/>
      </xsl:attribute>
    </xsl:if>
  </xsl:template>
  <xsl:template name="format">
    <xsl:if test="@format">
      <xsl:attribute name="js-format">
        <xsl:value-of select="@format"/>
      </xsl:attribute>
    </xsl:if>
  </xsl:template>
  <xsl:template name="keystroke">
    <xsl:if test="@keystroke">
      <xsl:attribute name="js-keystroke">
        <xsl:value-of select="@keystroke"/>
      </xsl:attribute>
    </xsl:if>
  </xsl:template>
  <!-- Other Form Field Attributes -->
  <xsl:template name="form_field_attributes">
    <xsl:call-template name="fieldname"/>
    <xsl:call-template name="readonly"/>
    <xsl:call-template name="maxlen"/>
    <xsl:call-template name="calculate"/>
    <xsl:call-template name="validate"/>
    <xsl:call-template name="format"/>
    <xsl:call-template name="keystroke"/>
  </xsl:template>
  <xsl:template name="maxlen">
    <xsl:if test="@maxlen">
      <xsl:attribute name="maxlen">
        <xsl:value-of select="@maxlen"/>
      </xsl:attribute>
    </xsl:if>
  </xsl:template>
  <xsl:template name="fieldname">
    <xsl:attribute name="name">
      <xsl:choose>
        <xsl:when test="@name">
          <xsl:value-of select="@name"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="generate-id(.)"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:attribute>
  </xsl:template>
  <xsl:template name="readonly">
    <xsl:if test="@readonly">
      <xsl:attribute name="readonly">
        <xsl:value-of select="@readonly"/>
      </xsl:attribute>
    </xsl:if>
  </xsl:template>
  <!-- Special XSL FO Element Handling -->
  <xsl:template match="pdf-insert">
    <xsl:processing-instruction name="xep-insert-document">
      <xsl:choose>
        <xsl:when test="@location">
          <xsl:value-of select="@location"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:text>prepend</xsl:text>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:text>:</xsl:text>
      <xsl:choose>
        <xsl:when test="starts-with(@href, 'http:') or starts-with(@href, 'https:')">
          <xsl:value-of select="@href"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:variable name="locref">
            <xsl:choose>
              <xsl:when test="starts-with(@href, '/')">
                <xsl:value-of select="substring-after(@href, '/')"/>
              </xsl:when>
              <xsl:otherwise>
                <xsl:value-of select="@href"/>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <xsl:value-of select="concat($base, $locref)"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:processing-instruction>
  </xsl:template>
  <xsl:template match="pdf-attachment">
    <rx:pdf-comment content="" opacity="0">
      <xsl:attribute name="title">
        <xsl:value-of select="@filename"/>
      </xsl:attribute>
      <rx:pdf-file-attachment>
        <xsl:attribute name="filename">
          <xsl:value-of select="@filename"/>
        </xsl:attribute>
        <xsl:attribute name="src">
          <xsl:choose>
            <xsl:when test="starts-with(@href, 'url(')">
              <xsl:value-of select="@href"/>
            </xsl:when>
            <xsl:when test="starts-with(@href, 'http:') or starts-with(@href, 'https:')">
              <xsl:text>url('</xsl:text>
              <xsl:value-of select="@href"/>
              <xsl:text>')</xsl:text>
            </xsl:when>
            <xsl:otherwise>
              <xsl:variable name="locref">
                <xsl:choose>
                  <xsl:when test="starts-with(@href, '/')">
                    <xsl:value-of select="substring-after(@href, '/')"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:value-of select="@href"/>
                  </xsl:otherwise>
                </xsl:choose>
              </xsl:variable>
              <xsl:text>url('</xsl:text>
              <xsl:value-of select="concat($base, $locref)"/>
              <xsl:text>')</xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
      </rx:pdf-file-attachment>
    </rx:pdf-comment>
  </xsl:template>
  <xsl:template match="comment">
    <rx:pdf-comment>
      <xsl:variable name="color">
        <xsl:call-template name="extractCSSStyle">
          <xsl:with-param name="css" select="@style"/>
          <xsl:with-param name="style" select="'color:'"/>
        </xsl:call-template>
      </xsl:variable>
      <xsl:attribute name="color">
        <xsl:choose>
          <xsl:when test="$color=''">
            <xsl:text>yellow</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="$color"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <xsl:variable name="opacity">
        <xsl:call-template name="extractCSSStyle">
          <xsl:with-param name="css" select="@style"/>
          <xsl:with-param name="style" select="'opacity:'"/>
        </xsl:call-template>
      </xsl:variable>
      <xsl:attribute name="opacity">
        <xsl:choose>
          <xsl:when test="$opacity=''">
            <xsl:text>1</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="$opacity"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
           <xsl:attribute name="content">
             <xsl:value-of select="descendant::comment-text[normalize-space()]"/>
      </xsl:attribute>
      <xsl:attribute name="title">
        <xsl:choose>
          <xsl:when test="descendant::title">
            <xsl:value-of select="descendant::title[normalize-space()]"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Comment</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <rx:pdf-sticky-note>
        <xsl:attribute name="icon-type">
          <xsl:choose>
            <xsl:when test="@icon-type">
              <xsl:value-of select="@icon-type"/>
            </xsl:when>
            <xsl:otherwise>
              <xsl:text>comment</xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
        <xsl:attribute name="open">
          <xsl:choose>
            <xsl:when test="@open">
              <xsl:value-of select="@open"/>
            </xsl:when>
            <xsl:otherwise>
              <xsl:text>false</xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
      </rx:pdf-sticky-note>
    </rx:pdf-comment>
  </xsl:template>
  <xsl:template match="footnote">
    <fo:footnote>
      <xsl:apply-templates select="node()"/>
    </fo:footnote>
  </xsl:template>
  <xsl:template match="footnote-body">
    <fo:footnote-body>
      <xsl:apply-templates select="node()"/>
    </fo:footnote-body>
  </xsl:template>
  <xsl:template match="inline">
    <fo:inline>
      <xsl:apply-templates select="node()"/>
    </fo:inline>
  </xsl:template>
  <xsl:template match="block">
    <fo:block>
      <xsl:apply-templates select="node()"/>
    </fo:block>
  </xsl:template>
  <xsl:template match="block-container">
    <fo:block-container>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <fo:block>
        <xsl:apply-templates select="node()"/>
      </fo:block>
    </fo:block-container>
  </xsl:template>
  <xsl:template match="pagenum">
    <fo:inline>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <fo:page-number/>
    </fo:inline>
  </xsl:template>
  <xsl:template match="totpages">
    <fo:inline>
      <xsl:call-template name="processAttr">
        <xsl:with-param name="elem" select="."/>
        <xsl:with-param name="type" select="name()"/>
      </xsl:call-template>
      <fo:page-number-citation-last ref-id="xeponline-document"/>
    </fo:inline>
  </xsl:template>
  <!-- Special Processing for svg fills to support rgba -->
  <xsl:template match="@fill">
    <xsl:attribute name="fill">
      <xsl:choose>
        <xsl:when test="starts-with(., 'url')">
          <xsl:value-of select="."/>
        </xsl:when>
        <xsl:when test="contains(., 'rgba')">
          <xsl:call-template name="ProcessColor">
            <xsl:with-param name="color" select="."/>
          </xsl:call-template>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="."/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:attribute>
    <xsl:choose>
      <xsl:when test="contains(., 'rgba')">
        <xsl:attribute name="opacity">
          <xsl:value-of
            select="substring-before(substring-after(substring-after(substring-after(., ','), ','), ','), ')')"
          />
        </xsl:attribute>
      </xsl:when>
    </xsl:choose>
  </xsl:template>
  <!-- Bug fixes for Adobe Reader -->
  <xsl:template match="@stroke">
    <xsl:attribute name="stroke">
      <xsl:choose>
        <xsl:when test=". = '#000000'">
          <xsl:text>none</xsl:text>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="."/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:attribute>
  </xsl:template>
  <xsl:template match="@opacity">
    <xsl:attribute name="opacity">
      <xsl:choose>
        <xsl:when test="number(.) &lt; .001">
          <xsl:text>0.01</xsl:text>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="."/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:attribute>
  </xsl:template>
  <xsl:template match="@fill-opacity">
    <xsl:attribute name="fill-opacity">
      <xsl:choose>
        <xsl:when test="number(.) &lt; .001">
          <xsl:text>0.01</xsl:text>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="."/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:attribute>
  </xsl:template>
  <xsl:template match="@stroke-opacity">
    <xsl:attribute name="stroke-opacity">
      <xsl:choose>
        <xsl:when test="number(.) &lt; .001">
          <xsl:text>0.01</xsl:text>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="."/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:attribute>
  </xsl:template>

  <!-- Utility Templates -->
  <xsl:template name="processAttr">
    <xsl:param name="elem"/>
    <xsl:param name="type"/>
    <xsl:param name="float"/>
    <xsl:param name="ignoreHeight"/>
    <xsl:call-template name="processCSSStyle">
      <xsl:with-param name="cssString" select="concat($elem/@style, ' ')"/>
      <xsl:with-param name="type" select="$type"/>
      <xsl:with-param name="float" select="$float"/>
      <xsl:with-param name="ignoreHeight" select="$ignoreHeight"/>
    </xsl:call-template>
    <xsl:apply-templates select="$elem/@*[not(name() = 'style') or not(name() = 'fostyle')]"/>
    <xsl:if test="@fostyle">
      <xsl:call-template name="processCSSStyle">
        <xsl:with-param name="cssString" select="concat($elem/@fostyle, ' ')"/>
        <xsl:with-param name="type" select="$type"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>
  <xsl:template name="processCSSStyle">
    <xsl:param name="cssString"/>
    <xsl:param name="type"/>
    <xsl:param name="float"/>
    <xsl:param name="ignoreHeight"/>
    <xsl:call-template name="processCSSEntry">
      <xsl:with-param name="attr" select="normalize-space(substring-before($cssString, ': '))"/>
      <xsl:with-param name="value"
        select="normalize-space(substring-after(substring-before($cssString, '; '), ': '))"/>
      <xsl:with-param name="cssRemaining" select="substring-after($cssString, '; ')"/>
      <xsl:with-param name="type" select="$type"/>
      <xsl:with-param name="float" select="$float"/>
      <xsl:with-param name="ignoreHeight" select="$ignoreHeight"/>
    </xsl:call-template>
  </xsl:template>
  <xsl:template name="processCSSEntry">
    <xsl:param name="attr"/>
    <xsl:param name="value"/>
    <xsl:param name="cssRemaining"/>
    <xsl:param name="type"/>
    <xsl:param name="float"/>
    <xsl:param name="ignoreHeight"/>
    <xsl:if test="$attr">
      <xsl:call-template name="handleAttr">
        <xsl:with-param name="attr" select="$attr"/>
        <xsl:with-param name="value" select="$value"/>
        <xsl:with-param name="type" select="$type"/>
        <xsl:with-param name="float" select="$float"/>
        <xsl:with-param name="ignoreHeight" select="$ignoreHeight"/>
      </xsl:call-template>
      <xsl:call-template name="processCSSEntry">
        <xsl:with-param name="attr" select="normalize-space(substring-before($cssRemaining, ': '))"/>
        <xsl:with-param name="value"
          select="normalize-space(substring-after(substring-before($cssRemaining, '; '), ': '))"/>
        <xsl:with-param name="cssRemaining" select="substring-after($cssRemaining, '; ')"/>
        <xsl:with-param name="type" select="$type"/>
        <xsl:with-param name="float" select="$float"/>
        <xsl:with-param name="ignoreHeight" select="$ignoreHeight"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>
  <xsl:template name="handleAttr">
    <xsl:param name="attr"/>
    <xsl:param name="value"/>
    <xsl:param name="type"/>
    <xsl:param name="float"/>
    <xsl:param name="ignoreHeight"/>
    <!-- CSS to XSL FO Exception Handling -->
    <xsl:choose>
      <xsl:when test="starts-with($value, '-moz')"/>
      <xsl:when test="starts-with($attr, '-')"/>
      <xsl:when test="starts-with($attr, 'data-')"/>
      <xsl:when test="contains($attr, '-webkit')"/>
      <xsl:when test="$attr = 'float' and $type = 'img'"/>
      <xsl:when test="$attr = 'overflow'"/>
      <xsl:when test="($attr = 'widows') or ($attr = 'orphans')">
        <xsl:attribute name="{$attr}">
          <xsl:choose>
            <xsl:when test="number($value) = $value">
              <xsl:value-of select="$value"/>
            </xsl:when>
            <xsl:otherwise>
              <xsl:text>3</xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
      </xsl:when>
      <xsl:when test="$attr = 'background-color'">
        <!-- background-color: rgba(0, 0, 0, 0); -->
        <xsl:if test="($value != 'rgba(0, 0, 0, 0)') and ($value != 'transparent')">
          <xsl:attribute name="{$attr}">
            <xsl:value-of select="$value"/>
          </xsl:attribute>
        </xsl:if>
      </xsl:when>
      <xsl:when test="$attr = 'width'">
        <xsl:choose>
          <xsl:when test="$type = 'img'">
            <xsl:attribute name="content-width">
              <xsl:value-of select="$value"/>
            </xsl:attribute>
          </xsl:when>
          <xsl:when test="$value = '0px'">
            <!-- do nothing, comes through on hidden divs when we want the content -->
          </xsl:when>
          <xsl:when
            test="$type = 'table' or $type = 'td' or $type = 'th' or $type = 'tbody' or $type = 'thead' or $type = 'tfoot'">
            <!-- do nothing, comes through on col elements -->
          </xsl:when>
          <xsl:when test="$float = 'true'">
            <!-- on a float, set the width to remove the padding left and right -->
            <xsl:variable name="pad-left">
              <xsl:call-template name="extractCSSStyle">
                <xsl:with-param name="css" select="@style"/>
                <xsl:with-param name="style" select="'padding-left:'"/>
              </xsl:call-template>
            </xsl:variable>
            <xsl:variable name="pad-right">
              <xsl:call-template name="extractCSSStyle">
                <xsl:with-param name="css" select="@style"/>
                <xsl:with-param name="style" select="'padding-right:'"/>
              </xsl:call-template>
            </xsl:variable>
            <xsl:attribute name="width">
              <xsl:value-of select="$value"/>
              <xsl:if test="string-length($pad-left) > 0">
                <xsl:text> - </xsl:text>
                <xsl:value-of select="$pad-left"/>
              </xsl:if>
              <xsl:if test="string-length($pad-right) > 0">
                <xsl:text> - </xsl:text>
                <xsl:value-of select="$pad-right"/>
              </xsl:if>
            </xsl:attribute>
          </xsl:when>
          <xsl:otherwise>
            <xsl:attribute name="width">
              <xsl:value-of select="$value"/>
            </xsl:attribute>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:when test="$attr = 'height'">
        <xsl:choose>
          <xsl:when test="$type = 'img'">
            <xsl:attribute name="content-height">
              <xsl:value-of select="$value"/>
            </xsl:attribute>
          </xsl:when>
          <xsl:when
            test="$type = 'tr' or $type = 'td' or $type = 'th' or $type = 'thead' or $type = 'tfoot' or $type = 'tbody'">
            <!-- do nothing, let formatter do its job, some browsers report wrong height -->
            <!-- This is in place now for only IE -->
          </xsl:when>
          <xsl:when test="$float = 'true' and $ignoreHeight = 'true'">
            <!-- do nothing, let formatter do its job, some browsers report wrong height -->
          </xsl:when>
          <xsl:when test="$value = '0px'">
            <!-- do nothing, comes through on hidden divs when we want the content -->
          </xsl:when>
          <xsl:otherwise>
            <xsl:attribute name="height">
              <xsl:value-of select="$value"/>
            </xsl:attribute>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:when test="$attr = 'vertical-align'">
        <xsl:choose>
          <xsl:when test="$type = 'td' or $type = 'th' or $type = 'tbody'">
            <xsl:attribute name="display-align">
              <xsl:choose>
                <xsl:when test="$value = 'middle'">
                  <xsl:text>center</xsl:text>
                </xsl:when>
                <xsl:when test="$value = 'top'">
                  <xsl:text>before</xsl:text>
                </xsl:when>
                <xsl:when test="$value = 'bottom'">
                  <xsl:text>after</xsl:text>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:value-of select="$value"/>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:attribute>
          </xsl:when>
          <xsl:otherwise>
            <xsl:attribute name="vertical-align">
              <xsl:value-of select="$value"/>
            </xsl:attribute>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:when test="$attr = 'list-style-type'">
        <!-- Ignore here, the actual implementation is on the li -->
      </xsl:when>
      <xsl:when test="$attr = 'list-style-image'">
        <!-- Ignore here, the actual implementation is on the li -->
      </xsl:when>
      <xsl:when test="$attr = 'margin-top'">
        <!-- Divide by 2 for HTML to FO -->
        <xsl:attribute name="margin-top">
          <xsl:choose>
            <xsl:when test="$type = 'inline'">
              <xsl:value-of select="$value"/>
            </xsl:when>
            <xsl:otherwise>
              <xsl:variable name="newval" select="number(translate($value, 'px', '')) div 2"/>
              <xsl:value-of select="concat($newval, 'px')"/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
      </xsl:when>
      <xsl:when test="$attr = 'margin-bottom'">
        <!-- Divide by 2 for HTML to FO -->
        <xsl:attribute name="margin-bottom">
          <xsl:choose>
            <xsl:when test="$type = 'inline'">
              <xsl:value-of select="$value"/>
            </xsl:when>
            <xsl:otherwise>
              <xsl:variable name="newval" select="number(translate($value, 'px', '')) div 2"/>
              <xsl:value-of select="concat($newval, 'px')"/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
      </xsl:when>
      <xsl:when test="$attr = 'margin-left'">
        <xsl:attribute name="margin-left">
          <xsl:value-of select="$value"/>
        </xsl:attribute>
      </xsl:when>
      <xsl:when test="$attr = 'margin-right'">
        <xsl:attribute name="margin-right">
          <xsl:value-of select="$value"/>
        </xsl:attribute>
      </xsl:when>
      <xsl:when test="$attr = 'background-image'">
        <xsl:choose>
          <xsl:when test="contains($value, 'linear-gradient(')">
            <!-- Do nothing, the color will be in the background-color -->
          </xsl:when>
          <xsl:otherwise>
            <xsl:attribute name="background-image">
              <xsl:value-of select="$value"/>
            </xsl:attribute>
            <!-- FO default is "repeat", override to no-repeat for incoming HTML -->
            <xsl:attribute name="background-repeat">
              <xsl:text>no-repeat</xsl:text>
            </xsl:attribute>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:when test="$attr = 'letter-spacing'">
        <xsl:if test="$type != 'img'">
          <xsl:attribute name="letter-spacing">
            <xsl:value-of select="$value"/>
          </xsl:attribute>
        </xsl:if>
      </xsl:when>
      <xsl:when test="$attr = 'display'"/>
      <xsl:when test="$attr = 'cursor'"/>
      <!-- Safari hack -->
      <xsl:when test="$attr = 'background-repeat'">
        <xsl:attribute name="background-repeat">
          <xsl:choose>
            <xsl:when test="string-length(substring-before($value, ' ')) > 0">
              <xsl:value-of select="substring-before($value, ' ')"/>
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="$value"/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
      </xsl:when>
      <xsl:when test="$attr = 'table-omit-initial-header'">
        <xsl:attribute name="rx:table-omit-initial-header">
          <xsl:value-of select="$value"/>
        </xsl:attribute>
      </xsl:when>
      <xsl:when test="$attr = 'text-shadow'">
        <xsl:attribute name="text-shadow">
          <xsl:choose>
            <xsl:when test="contains($value, 'rgba')">
              <xsl:call-template name="ProcessColor">
                <xsl:with-param name="color" select="$value"/>
              </xsl:call-template>
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="$value"/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:attribute>
      </xsl:when>
      <!-- These are fostyle only, adding rx: extensions that cannot be passed in -->
      <xsl:when test="$attr = 'background-image-scaling'">
        <xsl:attribute name="rx:background-image-scaling">
          <xsl:value-of select="$value"/>
        </xsl:attribute>
      </xsl:when>
      <xsl:when test="$attr = 'background-content-height'">
        <xsl:attribute name="rx:background-content-height">
          <xsl:value-of select="$value"/>
        </xsl:attribute>
      </xsl:when>
      <xsl:when test="$attr = 'background-content-width'">
        <xsl:attribute name="rx:background-content-width">
          <xsl:value-of select="$value"/>
        </xsl:attribute>
      </xsl:when>
      <xsl:otherwise>
        <xsl:attribute name="{$attr}">
          <xsl:value-of select="$value"/>
        </xsl:attribute>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <!--  rgba(255, 255, 255, 0.2) 0px 1px 0px -->
  <xsl:template name="ProcessColor">
    <xsl:param name="color"/>
    <xsl:choose>
      <xsl:when test="contains($color, 'rgba')">
        <xsl:value-of select="substring-before($color, 'rgba')"/>
        <xsl:text>rgb(</xsl:text>
        <!-- 255, 255, 255, 0.2 -->
        <xsl:variable name="colorcode"
          select="substring-before(substring-after($color, 'rgba('), ')')"/>
        <xsl:variable name="r" select="normalize-space(substring-before($colorcode, ','))"/>
        <xsl:variable name="g"
          select="normalize-space(substring-before(substring-after($colorcode, ','), ','))"/>
        <xsl:variable name="btemp" select="substring-after($colorcode, $g)"/>
        <xsl:variable name="b"
          select="normalize-space(substring-before(substring-after($btemp, ','), ','))"/>
        <xsl:value-of select="$r"/>
        <xsl:text>, </xsl:text>
        <xsl:value-of select="$g"/>
        <xsl:text>, </xsl:text>
        <xsl:value-of select="$b"/>
        <xsl:value-of select="substring-after($color, $colorcode)"/>
      </xsl:when>
    </xsl:choose>
  </xsl:template>
  <xsl:template match="@face">
    <xsl:attribute name="font-family">
      <xsl:value-of select="."/>
    </xsl:attribute>
  </xsl:template>
  <xsl:template match="@src">
    <xsl:attribute name="src">
      <xsl:choose>
        <xsl:when test="starts-with(., 'url(')">
          <xsl:value-of select="."/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:text>url('</xsl:text>
          <xsl:value-of select="."/>
          <xsl:text>')</xsl:text>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:attribute>
  </xsl:template>
  <xsl:template name="extractCSSStyle">
    <xsl:param name="css"/>
    <xsl:param name="style"/>
    <!-- OK, options here ...
      You have to make sure that you get *only* this attribute
      You cannot just search the string "width:" for example as it would match "border-width:"
      You cannot just preface a blank before the search, aka " width:" because it it starts with style="width: it would not match 
      -->
    <xsl:choose>
      <xsl:when
        test="string-length(normalize-space(substring-before(substring-after($css, concat(' ', $style)), ';'))) > 0">
        <xsl:value-of
          select="normalize-space(substring-before(substring-after($css, concat(' ', $style)), ';'))"
        />
      </xsl:when>
      <xsl:when test="starts-with($css, $style)">
        <xsl:value-of select="normalize-space(substring-before(substring-after($css, $style), ';'))"
        />
      </xsl:when>
    </xsl:choose>
  </xsl:template>
  <xsl:template name="isVisible">
    <xsl:variable name="display">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="@style"/>
        <xsl:with-param name="style" select="'display:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:choose>
      <xsl:when test="$display = 'none'">
        <xsl:value-of select="false()"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="true()"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="isBlock">
    <xsl:variable name="block">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="@style"/>
        <xsl:with-param name="style" select="'display:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:choose>
      <xsl:when test="$block = 'block'">
        <xsl:value-of select="true()"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="false()"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>


  <xsl:template name="isPositioned">
    <xsl:variable name="positioned">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="@style"/>
        <xsl:with-param name="style" select="'position:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:value-of select="$positioned"/>
  </xsl:template>
  <xsl:template name="floatType">
    <xsl:variable name="float">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="@style"/>
        <xsl:with-param name="style" select="'float:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:choose>
      <xsl:when test="$float = ''">
        <xsl:value-of select="'none'"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$float"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <!-- Older attributes some people still use -->
  <xsl:template match="@align">
    <xsl:attribute name="text-align">
      <xsl:value-of select="."/>
    </xsl:attribute>
  </xsl:template>
  <xsl:template match="@cellpadding">
    <xsl:attribute name="border-collapse">separate</xsl:attribute>
    <xsl:attribute name="border-separation">
      <xsl:value-of select="concat(., 'px')"/>
    </xsl:attribute>
  </xsl:template>
  <!-- Eliminate certain elements and attributes, they are either handled or need to be ignored -->
  <xsl:template
    match="pdfproperties | header | footer | iframe | script | noscript | embed | param | map | area | canvas | audio | track | source | nav"/>
  <xsl:template match="ms-widget | ms-widget-front">
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template
    match="@layout | @flex | @data | @value | @page-background | @name | @type | @cellspacing | @cursor | @border | @class | @alt | @base | @display | @onclick | @target | @float | @page-width | @page-height | @page-margin-right | @page-margin-left | @page-margin-top | @page-margin-bottom | @style[not(ancestor::svg:svg)] | @fostyle | @data-highcharts-chart"/>
  <!-- Unknown element match for generic XML -->
  <xsl:template match="node()" priority="-10">
    <xsl:variable name="display">
      <xsl:call-template name="extractCSSStyle">
        <xsl:with-param name="css" select="@style"/>
        <xsl:with-param name="style" select="'display:'"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:choose>
      <xsl:when test="$display = 'table'">
        <fo:table border-width="1px">
          <xsl:call-template name="processAttr">
            <xsl:with-param name="elem" select="."/>
            <xsl:with-param name="type" select="'table'"/>
          </xsl:call-template>
          <xsl:apply-templates/>
        </fo:table>
      </xsl:when>
      <xsl:when test="$display = 'table-row-group'">
        <fo:table-body>
          <xsl:call-template name="processAttr">
            <xsl:with-param name="elem" select="."/>
            <xsl:with-param name="type" select="'tbody'"/>
          </xsl:call-template>
          <xsl:apply-templates/>
        </fo:table-body>
      </xsl:when>
      <xsl:when test="$display = 'table-header-group'">
        <fo:table-header>
          <fo:table-row>
            <xsl:call-template name="processAttr">
              <xsl:with-param name="elem" select="."/>
              <xsl:with-param name="type" select="'thead'"/>
            </xsl:call-template>
            <xsl:apply-templates/>
          </fo:table-row>
        </fo:table-header>
      </xsl:when>
      <xsl:when test="$display = 'table-row'">
        <fo:table-row>
          <xsl:call-template name="processAttr">
            <xsl:with-param name="elem" select="."/>
            <xsl:with-param name="type" select="'tr'"/>
          </xsl:call-template>
          <xsl:apply-templates/>
        </fo:table-row>
      </xsl:when>
      <xsl:when test="$display = 'table-cell'">
        <fo:table-cell border-width="1px">
          <xsl:call-template name="processAttr">
            <xsl:with-param name="elem" select="."/>
            <xsl:with-param name="type" select="'td'"/>
          </xsl:call-template>
          <fo:block>
            <xsl:apply-templates/>
          </fo:block>
        </fo:table-cell>
      </xsl:when>
      <xsl:when test="$display = 'block' and not(ancestor::svg:svg)">
        <fo:block margin="0">
          <xsl:call-template name="processAttr">
            <xsl:with-param name="elem" select="."/>
            <xsl:with-param name="type" select="name()"/>
          </xsl:call-template>
          <xsl:apply-templates/>
        </fo:block>
      </xsl:when>
      <xsl:when test="$display = 'inline' and not(ancestor::svg:svg)">
        <fo:inline>
          <xsl:call-template name="processAttr">
            <xsl:with-param name="elem" select="."/>
            <xsl:with-param name="type" select="name()"/>
          </xsl:call-template>
          <xsl:apply-templates/>
        </fo:inline>
      </xsl:when>
      <xsl:otherwise>
        <xsl:copy>
          <xsl:apply-templates select="@* | node()"/>
        </xsl:copy>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="generate-bookmarks">
    <xsl:if test="count(div//h1) > 0">
      <fo:bookmark-tree>
        <xsl:for-each select="/div//h1">
          <xsl:variable name="current-h1" select="generate-id()"/>
          <fo:bookmark starting-state="hide">
            <xsl:attribute name="internal-destination">
              <xsl:choose>
                <xsl:when test="@id">
                  <xsl:value-of select="@id"/>
                </xsl:when>
                <xsl:when
                  test="
                    name(preceding-sibling::*[1]) = 'a' and
                    preceding-sibling::*[1][@name]">
                  <xsl:value-of select="preceding-sibling::*[1]/@name"/>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:value-of select="$current-h1"/>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:attribute>
            <fo:bookmark-title>
              <xsl:value-of select="."/>
            </fo:bookmark-title>
            <xsl:for-each select="following-sibling::h2">
              <xsl:variable name="current-h2" select="generate-id()"/>
              <xsl:if test="generate-id(preceding-sibling::h1[1]) = $current-h1">
                <fo:bookmark starting-state="hide">
                  <xsl:attribute name="internal-destination">
                    <xsl:choose>
                      <xsl:when test="@id">
                        <xsl:value-of select="@id"/>
                      </xsl:when>
                      <xsl:otherwise>
                        <xsl:value-of select="$current-h2"/>
                      </xsl:otherwise>
                    </xsl:choose>
                  </xsl:attribute>
                  <fo:bookmark-title>
                    <xsl:value-of select="."/>
                  </fo:bookmark-title>
                  <xsl:for-each select="following-sibling::h3">
                    <xsl:variable name="current-h3" select="generate-id()"/>
                    <xsl:if test="generate-id(preceding-sibling::h2[1]) = $current-h2">
                      <fo:bookmark starting-state="hide">
                        <xsl:attribute name="internal-destination">
                          <xsl:choose>
                            <xsl:when test="@id">
                              <xsl:value-of select="@id"/>
                            </xsl:when>
                            <xsl:otherwise>
                              <xsl:value-of select="$current-h3"/>
                            </xsl:otherwise>
                          </xsl:choose>
                        </xsl:attribute>
                        <fo:bookmark-title>
                          <xsl:value-of select="."/>
                        </fo:bookmark-title>
                        <xsl:for-each select="following-sibling::h4">
                          <xsl:if test="generate-id(preceding-sibling::h3[1]) = $current-h3">
                            <fo:bookmark starting-state="hide">
                              <xsl:attribute name="internal-destination">
                                <xsl:choose>
                                  <xsl:when test="@id">
                                    <xsl:value-of select="@id"/>
                                  </xsl:when>
                                  <xsl:otherwise>
                                    <xsl:value-of select="generate-id()"/>
                                  </xsl:otherwise>
                                </xsl:choose>
                              </xsl:attribute>
                              <fo:bookmark-title>
                                <xsl:value-of select="."/>
                              </fo:bookmark-title>
                            </fo:bookmark>
                          </xsl:if>
                        </xsl:for-each>
                      </fo:bookmark>
                    </xsl:if>
                  </xsl:for-each>
                </fo:bookmark>
              </xsl:if>
            </xsl:for-each>
          </fo:bookmark>
        </xsl:for-each>
      </fo:bookmark-tree>
    </xsl:if>
  </xsl:template>



  <!-- All other attributes and text -->
  <xsl:template match="@* | text()">
    <xsl:copy/>
  </xsl:template>
</xsl:stylesheet>