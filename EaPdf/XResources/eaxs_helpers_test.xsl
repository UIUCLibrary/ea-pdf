<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
    xmlns:fn="http://www.w3.org/2005/xpath-functions"
    xmlns:my="http://library.illinois.edu/myFunctions"
    >


    <xsl:param name="test-helpers">false</xsl:param>
    
    <xsl:template name="test-helpers">
        <xsl:if test="fn:lower-case(fn:normalize-space($test-helpers)) = 'true'">
            <xsl:message>TESTING MY FUNCTIONS</xsl:message>
            
            <xsl:if test="not(my:ScaleLength(400,100) = 100)">
                <xsl:message terminate="yes">  ERROR: my:ScaleLength(400,100) does not equal 100</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ScaleLength(100,400) = 100)">
                <xsl:message terminate="yes"> ERROR: my:ScaleLength(100,400) does not equal 100</xsl:message>
            </xsl:if>
            
            <xsl:if test="not(my:ConvertToPoints('2') = 2)">
                <xsl:message terminate="yes"> ERROR: my:ConvertToPoints('1000') does not equal 1000</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ConvertToPoints('2pt') = 2)">
                <xsl:message terminate="yes"> ERROR: my:ConvertToPoints('1000pt') does not equal 1000</xsl:message>
            </xsl:if>
             <xsl:if test="not(my:ConvertToPoints('2in') = 144)">
                <xsl:message terminate="yes"> ERROR: my:ConvertToPoints('2in') does not equal 144</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ConvertToPoints('2cm') = 56.69291338582677)">
                <xsl:message terminate="yes"> ERROR: my:ConvertToPoints('2cm') does not equal 56.69291338582677</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ConvertToPoints('2mm') = 5.669291338582677)">
                <xsl:message terminate="yes"> ERROR: my:ConvertToPoints('2mm') does not equal 5.669291338582677</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ConvertToPoints('2px') = 1.5)"><!-- assumes the default 96 dpi resolution -->
                <xsl:message terminate="yes"> ERROR: my:ConvertToPoints('2px') does not equal 1.5</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ConvertToPoints('2pc') = 24)">
                <xsl:message terminate="yes"> ERROR: my:ConvertToPoints('2pc') does not equal 24</xsl:message>
            </xsl:if>
            
            <xsl:if test="not(my:ScaleToBodyWidth('2em') = '2em')">
                <xsl:message terminate="yes"> ERROR: my:ScaleToBodyWidth('2em') does not equal '2em'</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ScaleToBodyWidth('2in') = '2in')">
                <xsl:message terminate="yes"> ERROR: my:ScaleToBodyWidth('2in') does not equal '2in'</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ScaleToBodyWidth('144pt') = '2in')">
                <xsl:message terminate="yes"> ERROR: my:ScaleToBodyWidth('144pt') does not equal '2in'</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ScaleToBodyWidth('20in') = '5.8333335in')"> <!-- assumes default page size and margins -->
                <xsl:message terminate="yes"> ERROR: my:ScaleToBodyWidth('20in') does not equal '5.8333335in'</xsl:message>
            </xsl:if>
            
            <xsl:if test="not(my:ScaleToBodyHeight('2em') = '2em')">
                <xsl:message terminate="yes"> ERROR: my:ScaleToBodyHeight('2em') does not equal '2em'</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ScaleToBodyHeight('2in') = '2in')">
                <xsl:message terminate="yes"> ERROR: my:ScaleToBodyHeight('2in') does not equal '2in'</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ScaleToBodyHeight('144pt') = '2in')">
                <xsl:message terminate="yes"> ERROR: my:ScaleToBodyHeight('144pt') does not equal '2in'</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:ScaleToBodyHeight('20in') = '8.75in')"> <!-- assumes default page size and margins -->
                <xsl:message terminate="yes"> ERROR: my:ScaleToBodyHeight('20in') does not equal '8.75in'</xsl:message>
            </xsl:if>
            
            <xsl:if test="not(my:StyleContainsProperty('overflow:hidden','width') = '')"> 
                <xsl:message terminate="yes"> ERROR: my:StyleContainsProperty('overflow:hidden','width') does not equal ''</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:StyleContainsProperty('overflow:hidden','overflow') = 'hidden')"> 
                <xsl:message terminate="yes"> ERROR: my:StyleContainsProperty('overflow:hidden','overflow') does not equal 'hidden'</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:StyleContainsProperty(' overflow: hidden ',' overflow ') = 'hidden')"> 
                <xsl:message terminate="yes"> ERROR: my:StyleContainsProperty(' overflow: hidden ',' overflow ') does not equal 'hidden'</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:StyleContainsProperty(' overflow : hidden ','overflow') = 'hidden')"> 
                <xsl:message terminate="yes"> ERROR: my:StyleContainsProperty(' overflow : hidden ','overflow') does not equal 'hidden'</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:StyleContainsProperty(' width:100 ; overflow: hidden; height: 200 ','overflow') = 'hidden')"> 
                <xsl:message terminate="yes"> ERROR: my:StyleContainsProperty(' width:100 ; overflow: hidden; height: 200 ','overflow') does not equal 'hidden'</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:StyleContainsProperty(' width:100 ; overflow: hidden; height: 200 ','width') = '100')"> 
                <xsl:message terminate="yes"> ERROR: my:StyleContainsProperty(' width:100 ; overflow: hidden; height: 200 ','width') does not equal '100'</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:StyleContainsProperty(' width:100 ; overflow: hidden; height: 200 ','height') = '200')"> 
                <xsl:message terminate="yes"> ERROR: my:StyleContainsProperty(' width:100 ; overflow: hidden; height: 200 ','height') does not equal '200'</xsl:message>
            </xsl:if>
            <xsl:if test="my:StyleContainsProperty(' width:100 ; overflow: hidden; height: 200 ','scale')"> 
                <xsl:message terminate="yes"> ERROR: my:StyleContainsProperty(' width:100 ; overflow: hidden; height: 200 ','scale') is not false</xsl:message>
            </xsl:if>
            <xsl:if test="not(my:StyleContainsProperty(' width:100 ; overflow: hidden; height: 200 ','overflow'))"> 
                <xsl:message terminate="yes"> ERROR: my:StyleContainsProperty(' width:100 ; overflow: hidden; height: 200 ','overflow') is not true</xsl:message>
            </xsl:if>
                       
            <xsl:message>TESTING MY FUNCTIONS FINISHED -- NO ERRORS</xsl:message>
        </xsl:if>
    </xsl:template>
    
    
</xsl:stylesheet>
