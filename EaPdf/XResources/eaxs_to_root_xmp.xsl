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
	<xsl:variable name="pdf_a_conf_level_values" select="('A','B','U')"/>	
	<xsl:variable name="pdf_a_conf_level_norm" select="fn:upper-case(normalize-space($pdf_a_conf_level))"/>
	
	<xsl:param name="pdfmailid_version">1</xsl:param>
	<xsl:param name="pdfmailid_rev">2024</xsl:param>
	<xsl:param name="pdfmailid_conformance">m</xsl:param>
	<xsl:variable name="pdfmailid_conformance_values" select="('s', 'si', 'm', 'mi', 'c', 'ci')"/>
	<xsl:variable name="pdfmailid_conformance_norm" select="fn:lower-case(normalize-space($pdfmailid_conformance))"/>
	
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
		<xsl:if test="not($pdf_a_conf_level_norm = $pdf_a_conf_level_values)">
			<xsl:message terminate="yes">The 'pdf_a_conf_level' param must be one of ('A','B','U'); it was '<xsl:value-of select="$pdf_a_conf_level_norm"/>'.</xsl:message>
		</xsl:if>		
		<xsl:if test="not($pdfmailid_conformance_norm = $pdfmailid_conformance_values)">
			<xsl:message terminate="yes">The 'pdfmailid_conformance' param must be one of ('s', 'si', 'm', 'mi', 'c', 'ci'); it was '<xsl:value-of select="$pdfmailid_conformance_norm"/>'.</xsl:message>
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
							<rdf:li xml:lang="x-default"><xsl:value-of select="$title"/> (<xsl:value-of select="$profile"/>)</rdf:li>
							<rdf:li xml:lang="en"><xsl:value-of select="$title"/> (<xsl:value-of select="$profile"/>)</rdf:li>
						</rdf:Alt>
					</dc:title>
					<dc:description>
						<rdf:Alt>
							<rdf:li xml:lang="x-default"><xsl:value-of select="$description"/></rdf:li>
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
					<pdf:Keywords>EA-PDF</pdf:Keywords>

					<pdfaid:part>3</pdfaid:part>
					<pdfaid:conformance><xsl:value-of select="$pdf_a_conf_level_norm"/></pdfaid:conformance>

					<pdfmailid:version><xsl:value-of select="$pdfmailid_version"/></pdfmailid:version>
					<pdfmailid:rev><xsl:value-of select="$pdfmailid_rev"/></pdfmailid:rev>
					<pdfmailid:conformance><xsl:value-of select="$pdfmailid_conformance"/></pdfmailid:conformance>

					<xmp:CreatorTool><xsl:value-of select="$creator"/></xmp:CreatorTool>
					<xmp:MetadataDate><xsl:value-of select="$datetime-string"/></xmp:MetadataDate>
					<xmp:CreateDate><xsl:value-of select="$datetime-string"/></xmp:CreateDate>
					<xmp:ModifyDate><xsl:value-of select="$datetime-string"/></xmp:ModifyDate>
					
					<pdfmailmeta:assets>
						<rdf:Seq>
							<xsl:for-each select="//eaxs:FolderProperties[eaxs:RelPath] | //eaxs:MessageProperties[eaxs:RelPath]">
								<rdf:li>
									<pdfmailmeta:Asset>
										<!-- The 'urn:hash:' is based on draft https://datatracker.ietf.org/doc/html/draft-thiemann-hash-urn-01 -->
										<xsl:attribute name="rdf:about">urn:hash:<xsl:value-of select="fn:lower-case(eaxs:Hash/eaxs:Function)"/>:<xsl:value-of select="fn:lower-case(eaxs:Hash/eaxs:Value)"/></xsl:attribute>
										<pdfmailmeta:filename><xsl:value-of select="eaxs:RelPath"/></pdfmailmeta:filename>
										<pdfmailmeta:sizeBytes><xsl:value-of select="eaxs:Size"/></pdfmailmeta:sizeBytes>
										<pdfmailmeta:format><xsl:value-of select="eaxs:ContentType"/></pdfmailmeta:format>
									</pdfmailmeta:Asset>
								</rdf:li>
							</xsl:for-each>
						</rdf:Seq>
					</pdfmailmeta:assets>

					<pdfmailmeta:email>
						<rdf:Seq>
							<xsl:for-each select="//eaxs:Folder/eaxs:Message">
								<rdf:li>
									<pdfmailmeta:Email>
										<!-- The 'mid:' URL is based on https://www.ietf.org/rfc/rfc2392.txt -->
										<xsl:attribute name="rdf:about">mid:<xsl:value-of select="fn:encode-for-uri(eaxs:MessageId)"/></xsl:attribute>
										<pdfmailmeta:guid><xsl:value-of select="eaxs:Guid"/></pdfmailmeta:guid>							
										<pdfmailmeta:messageid><xsl:value-of select="eaxs:MessageId"/></pdfmailmeta:messageid>							
										<xsl:if test="eaxs:Subject | eaxs:Keywords">
											<pdfmailmeta:subject>
												<rdf:Bag>
													<xsl:apply-templates select="eaxs:Subject"/>
													<xsl:apply-templates select="eaxs:Keywords"/>
												</rdf:Bag>
											</pdfmailmeta:subject>							
										</xsl:if>
										<xsl:if test="eaxs:Comments">
											<pdfmailmeta:comments>
												<rdf:Bag>
													<xsl:apply-templates select="eaxs:Comments"/>
												</rdf:Bag>
											</pdfmailmeta:comments>
										</xsl:if>
										<xsl:if test="eaxs:OrigDate">
											<pdfmailmeta:sent><xsl:value-of select="eaxs:OrigDate"/></pdfmailmeta:sent>
										</xsl:if>
										<!--
										<xsl:if test="eaxs:Sender">
											<pdfmailmeta:sender>
												<xsl:apply-templates select="eaxs:Sender"/>
											</pdfmailmeta:sender>
										</xsl:if>
										-->
										<xsl:if test="eaxs:From">
											<pdfmailmeta:from>
												<rdf:Seq>
													<xsl:for-each select="eaxs:From/*">
														<rdf:li>
															<xsl:apply-templates select="."/>
														</rdf:li>
													</xsl:for-each>
												</rdf:Seq>
											</pdfmailmeta:from>
										</xsl:if>
										<xsl:if test="eaxs:To">
											<pdfmailmeta:to>
												<rdf:Seq>
													<xsl:for-each select="eaxs:To/*">
														<rdf:li>
															<xsl:apply-templates select="."/>
														</rdf:li>
													</xsl:for-each>
												</rdf:Seq>
											</pdfmailmeta:to>
										</xsl:if>
										<xsl:if test="eaxs:Cc">
											<pdfmailmeta:cc>
												<rdf:Seq>
													<xsl:for-each select="eaxs:Cc/*">
														<rdf:li>
															<xsl:apply-templates select="."/>
														</rdf:li>
													</xsl:for-each>
												</rdf:Seq>
											</pdfmailmeta:cc>
										</xsl:if>
										<xsl:if test="eaxs:Bcc">
											<pdfmailmeta:bcc>
												<rdf:Seq>
													<xsl:for-each select="eaxs:Bcc/*">
														<rdf:li>
															<xsl:apply-templates select="."/>
														</rdf:li>
													</xsl:for-each>
												</rdf:Seq>
											</pdfmailmeta:bcc>
										</xsl:if>
										<xsl:if test="eaxs:InReplyTo">
											<pdfmailmeta:inReplyTo>
												<rdf:Seq>
													<xsl:apply-templates select="eaxs:InReplyTo"/>
												</rdf:Seq>
											</pdfmailmeta:inReplyTo>
										</xsl:if>
										<xsl:if test="eaxs:References">
											<pdfmailmeta:references>
												<rdf:Seq>
													<xsl:apply-templates select="eaxs:References"/>
												</rdf:Seq>
											</pdfmailmeta:references>							
										</xsl:if>
										<xsl:if test=".//eaxs:ContentType">
											<pdfmailmeta:contentType><xsl:value-of select="(.//eaxs:ContentType)[1]"/></pdfmailmeta:contentType>							
										</xsl:if>
										<xsl:if test="eaxs:MessageProperties/eaxs:Size">
											<pdfmailmeta:sizeBytes><xsl:value-of select="eaxs:MessageProperties/eaxs:Size"/></pdfmailmeta:sizeBytes>							
										</xsl:if>
										<pdfmailmeta:attachmentCount><xsl:value-of select="count(.//eaxs:*[@IsAttachment='true'])"/></pdfmailmeta:attachmentCount>							
									</pdfmailmeta:Email>
								</rdf:li>
							</xsl:for-each>
						</rdf:Seq>
					</pdfmailmeta:email>
				</rdf:Description>
				
				<xsl:copy-of select="document('EaPdfXmpSchema.xmp')/rdf:RDF/rdf:Description"  />
				
			</rdf:RDF>
		</x:xmpmeta>
		<xsl:processing-instruction name="xpacket">end="r"</xsl:processing-instruction>	
	</xsl:template>

	<xsl:template match="eaxs:Mailbox | eaxs:Sender" xmlns:foaf="http://xmlns.com/foaf/0.1/">
		<foaf:Agent>
			<xsl:if test="@name">
				<foaf:name><xsl:value-of select="@name"/></foaf:name>												
			</xsl:if>
			<xsl:if test="@address">
				<foaf:mbox>mailto:<xsl:value-of select="@address"/></foaf:mbox>												
			</xsl:if>
			<xsl:if test="not(@address) and not(@name)">
				<foaf:name><xsl:value-of select="."/></foaf:name>
			</xsl:if>
		</foaf:Agent>
	</xsl:template>
	
	<xsl:template match="eaxs:Group" xmlns:foaf="http://xmlns.com/foaf/0.1/" xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" >
		<foaf:Agent>
			<foaf:name><xsl:value-of select="eaxs:Name"/></foaf:name>
			<xsl:if test="eaxs:Group | eaxs:Mailbox">
				<foaf:member>
					<rdf:Seq>
						<xsl:for-each select="eaxs:Group | eaxs:Mailbox">
							<rdf:li>
								<xsl:apply-templates select="."/>
							</rdf:li>
						</xsl:for-each>						
					</rdf:Seq>
				</foaf:member>
			</xsl:if>
		</foaf:Agent>
	</xsl:template>
	
	<xsl:template match="eaxs:Subject" xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
		<rdf:li><xsl:value-of select="."/></rdf:li>
	</xsl:template>
	
	<xsl:template match="eaxs:InReplyTo" xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
		<rdf:li><xsl:value-of select="."/></rdf:li>
	</xsl:template>

	<xsl:template match="eaxs:References" xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
		<rdf:li><xsl:value-of select="."/></rdf:li>
	</xsl:template>
	
	<xsl:template match="eaxs:Keywords" xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
		<rdf:li><xsl:value-of select="."/></rdf:li>
	</xsl:template>
	
	<xsl:template match="eaxs:Comments" xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
		<rdf:li><xsl:value-of select="."/></rdf:li>
	</xsl:template>
	
	
</xsl:stylesheet>
