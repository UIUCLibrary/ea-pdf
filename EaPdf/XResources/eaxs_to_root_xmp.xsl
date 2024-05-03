<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="2.0"
	xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"

	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:xs="http://www.w3.org/2001/XMLSchema"
	xmlns:fn="http://www.w3.org/2005/xpath-functions"
	
	exclude-result-prefixes="eaxs xsl xs fn"
	>

	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes" omit-xml-declaration="yes" />
	
	<xsl:param name="fo-processor-version">FOP Version 2.8</xsl:param> <!-- Values used: fop or xep -->
	
	<xsl:param name="pdf_a_conf_level">A</xsl:param><!-- A, B, or U -->
	<xsl:variable name="pdf_a_conf_level_norm" select="fn:upper-case(normalize-space($pdf_a_conf_level))"/>
	
	<xsl:param name="description">
		<xsl:text>PDF Email Archive </xsl:text>
		<xsl:if test="/eaxs:Account/eaxs:EmailAddress">
			<xsl:text>for Account(s) '</xsl:text>
			<xsl:value-of select="fn:string-join(/eaxs:Account/eaxs:EmailAddress,', ')"/>
			<xsl:text>'</xsl:text>
		</xsl:if>
		<xsl:text> for Folder(s) '</xsl:text>
		<xsl:value-of select="fn:string-join(/eaxs:Account/eaxs:Folder/eaxs:Name, ',')"/> 
		<xsl:text>'</xsl:text>
	</xsl:param>
	<xsl:param name="global-id" select="/eaxs:Account/eaxs:GlobalId"/>
	<!-- subtract one second so that the moddate ends up being different than the creation date; Adobe doesn't seem to properly display these if they are exactly the same -->
	<xsl:param name="datetime-string" select="fn:format-dateTime(fn:adjust-dateTime-to-timezone(fn:current-dateTime() - xs:dayTimeDuration('PT0H0M1S'),xs:dayTimeDuration('P0D')),'[Y0001]-[M01]-[D01]T[H01]:[m01]:[s01]Z')"/>
	
	<xsl:param name="title" select="'PDF Email Archive'"/>
	<xsl:param name="profile" select="'PDF/mail-1m'"/>
	<xsl:param name="creator" select="'UIUCLibrary.EaPdf'"/>
	<xsl:param name="producer"><xsl:value-of select="$fo-processor-version"/></xsl:param>
	
	<xsl:template match="/">
		<xsl:if test="$pdf_a_conf_level_norm != 'A' and $pdf_a_conf_level_norm != 'B' and $pdf_a_conf_level_norm != 'U'">
			<xsl:message terminate="yes">The 'pdf_a_conf_level' param must be 'A', 'B', or 'U'; it was '<xsl:value-of select="$pdf_a_conf_level_norm"/>'.</xsl:message>
		</xsl:if>
		<xsl:processing-instruction name="xpacket">begin="" id="W5M0MpCehiHzreSzNTczkc9d" </xsl:processing-instruction><xsl:text xml:space="preserve">
</xsl:text>
		<x:xmpmeta xmlns:x="adobe:ns:meta/">
			<rdf:RDF 
				xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" 
				>
				
				<rdf:Description rdf:about="" 	
					xmlns:dc="http://purl.org/dc/elements/1.1/"
					xmlns:dcterms="http://purl.org/dc/terms/"
					xmlns:foaf="http://xmlns.com/foaf/0.1/"
					
					xmlns:pdf="http://ns.adobe.com/pdf/1.3/"
					xmlns:pdfx="http://ns.adobe.com/pdfx/1.3/"
					xmlns:xmp="http://ns.adobe.com/xap/1.0/"
					
					xmlns:pdfaid="http://www.aiim.org/pdfa/ns/id/"
					
					xmlns:pdfmail="http://www.pdfa.org/eapdf/"
					xmlns:pdfmailid="http://www.pdfa.org/eapdf/ns/id/"
					xmlns:pdfmailmeta="http://www.pdfa.org/eapdf/ns/meta/"
					>
					<dc:title>
						<rdf:Alt>
							<rdf:li xml:lang="en"><xsl:value-of select="$title"/> (<xsl:value-of select="$profile"/>)</rdf:li>
						</rdf:Alt>
					</dc:title>
					<dc:description>
						<rdf:Alt>
							<rdf:li xml:lang="en"><xsl:value-of select="$description"/></rdf:li>
						</rdf:Alt>
					</dc:description>

					<dc:identifier><xsl:value-of select="$global-id"/></dc:identifier>

					<dc:format>application/pdf</dc:format>
					<dc:language>
						<rdf:Bag>
							<rdf:li>x-unknown</rdf:li>
						</rdf:Bag>
					</dc:language>
					<dc:date>
						<rdf:Seq>
							<rdf:li><xsl:value-of select="$datetime-string"/></rdf:li>
						</rdf:Seq>
					</dc:date>

					<pdf:Producer><xsl:value-of select="$producer"/></pdf:Producer>
					<pdf:PDFVersion>1.7</pdf:PDFVersion>

					<pdfaid:part>3</pdfaid:part>
					<pdfaid:conformance><xsl:value-of select="$pdf_a_conf_level_norm"/></pdfaid:conformance>

					<pdfmailid:part>1</pdfmailid:part>
					<pdfmailid:rev>2022</pdfmailid:rev>
					<pdfmailid:conformance>m</pdfmailid:conformance>

					<xmp:CreatorTool><xsl:value-of select="$creator"/></xmp:CreatorTool>
					<xmp:MetadataDate><xsl:value-of select="$datetime-string"/></xmp:MetadataDate>
					<xmp:CreateDate><xsl:value-of select="$datetime-string"/></xmp:CreateDate>
					<xmp:ModifyDate><xsl:value-of select="$datetime-string"/></xmp:ModifyDate>
				</rdf:Description>
				
				<xsl:copy-of select="document('EaPdfXmpSchema.xmp')/rdf:RDF/rdf:Description"  />
				
			</rdf:RDF>
		</x:xmpmeta>
		<xsl:processing-instruction name="xpacket">end="r"</xsl:processing-instruction>	
	</xsl:template>


</xsl:stylesheet>
