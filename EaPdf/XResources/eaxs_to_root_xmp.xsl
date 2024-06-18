<?xml version="1.1" encoding="utf-8"?>

<xsl:stylesheet version="2.0"
	xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"

	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:xs="http://www.w3.org/2001/XMLSchema"
	xmlns:fn="http://www.w3.org/2005/xpath-functions"
	
	exclude-result-prefixes="eaxs xsl xs fn"
	>

	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes" omit-xml-declaration="yes" />
	
	<xsl:include href="eaxs_mime_helpers.xsl"/>

	<xsl:param name="fo-processor-version">FOP Version 2.8</xsl:param> <!-- Values used: fop or xep -->
	
	<xsl:param name="pdf_a_conf_level">A</xsl:param><!-- A, B, or U -->
	<xsl:variable name="pdf_a_conf_level_values" select="('A','B','U')"/>	
	<xsl:variable name="pdf_a_conf_level_norm" select="fn:upper-case(normalize-space($pdf_a_conf_level))"/>
	
	<xsl:variable name="pdfmailid_version">1</xsl:variable>
	<xsl:variable name="pdfmailid_rev">2024</xsl:variable>
	<xsl:variable name="pdfmailid_conformance">
		<xsl:choose>
			<xsl:when test="count(//eaxs:Message) &lt;= 1">s</xsl:when>
			<xsl:otherwise>m</xsl:otherwise>
		</xsl:choose>
	</xsl:variable>
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
			<xsl:message terminate="yes">ERROR: The 'pdf_a_conf_level' param must be one of ('A','B','U'); it was '<xsl:value-of select="$pdf_a_conf_level_norm"/>'.</xsl:message>
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
										<pdfmailmeta:Filename><xsl:value-of select="eaxs:RelPath"/></pdfmailmeta:Filename>
										<pdfmailmeta:SizeInBytes><xsl:value-of select="eaxs:Size"/></pdfmailmeta:SizeInBytes>
										<pdfmailmeta:Format><xsl:value-of select="eaxs:ContentType"/></pdfmailmeta:Format>
										<xsl:choose>
											<xsl:when test="local-name(.) = 'FolderProperties'">
												<pdfmailmeta:NumberMessages><xsl:value-of select="count(../eaxs:Message)"/></pdfmailmeta:NumberMessages>												
											</xsl:when>
											<xsl:when test="local-name(.) = 'MessageProperties'">
												<pdfmailmeta:NumberMessages>1</pdfmailmeta:NumberMessages>
											</xsl:when>
											<xsl:otherwise>
												<xsl:message terminate="yes">ERROR: Unexpected element '<xsl:value-of select="local-name(.)"/>'.</xsl:message>
											</xsl:otherwise>
										</xsl:choose>
										<xsl:choose>
											<xsl:when test="fn:upper-case(normalize-space(eaxs:Hash/eaxs:Function))='MD5'">
												<pdfmailmeta:CheckSum><xsl:value-of select="fn:lower-case(normalize-space(eaxs:Hash/eaxs:Value))"/></pdfmailmeta:CheckSum>
											</xsl:when>
											<xsl:otherwise>
												<xsl:message terminate="no">WARNING: Unsupported hash algorithm '<xsl:value-of select="eaxs:Hash/eaxs:Function"/>'</xsl:message>
											</xsl:otherwise>
										</xsl:choose>
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
										
										<pdfmailmeta:Asset>
											<xsl:choose>
												<xsl:when test="eaxs:MessageProperties[eaxs:RelPath]">
													<!-- The message came from one source file, like an EML or MSG file -->
													<xsl:attribute name="rdf:resource">urn:hash:<xsl:value-of select="fn:lower-case(eaxs:MessageProperties/eaxs:Hash/eaxs:Function)"/>:<xsl:value-of select="fn:lower-case(eaxs:MessageProperties/eaxs:Hash/eaxs:Value)"/></xsl:attribute>
												</xsl:when>
												<xsl:otherwise>
													<!-- The message can from an aggregation file like an MBOX, find the first folder ancestor with a RelPath -->
													<xsl:attribute name="rdf:resource">urn:hash:<xsl:value-of select="fn:lower-case((ancestor::eaxs:Folder/eaxs:FolderProperties[eaxs:RelPath])[1]/eaxs:Hash/eaxs:Function)"/>:<xsl:value-of select="fn:lower-case((ancestor::eaxs:Folder/eaxs:FolderProperties[eaxs:RelPath])[1]/eaxs:Hash/eaxs:Value)"/></xsl:attribute>
												</xsl:otherwise>
											</xsl:choose>
										</pdfmailmeta:Asset>
										<pdfmailmeta:AssetFilename>
											<xsl:choose>
												<xsl:when test="eaxs:MessageProperties[eaxs:RelPath]">
													<!-- The message came from one source file, like an EML or MSG file -->
													<xsl:value-of select="eaxs:MessageProperties/eaxs:RelPath"/>
												</xsl:when>
												<xsl:otherwise>
													<!-- The message can from an aggregation file like an MBOX, find the first folder ancestor with a RelPath -->
													<xsl:value-of select="(ancestor::eaxs:Folder/eaxs:FolderProperties[eaxs:RelPath])[1]/eaxs:RelPath"/>
												</xsl:otherwise>
											</xsl:choose>
										</pdfmailmeta:AssetFilename>
										<xsl:if test="eaxs:To">
											<pdfmailmeta:To>
												<rdf:Seq>
													<xsl:for-each select="eaxs:To/*">
														<rdf:li>
															<xsl:apply-templates select="."/>
														</rdf:li>
													</xsl:for-each>
												</rdf:Seq>
											</pdfmailmeta:To>
										</xsl:if>
										<xsl:if test="eaxs:From">
											<pdfmailmeta:From>
												<rdf:Seq>
													<xsl:for-each select="eaxs:From/*">
														<rdf:li>
															<xsl:apply-templates select="."/>
														</rdf:li>
													</xsl:for-each>
												</rdf:Seq>
											</pdfmailmeta:From>
										</xsl:if>
										<xsl:if test="eaxs:Sender">
											<pdfmailmeta:Sender>
												<xsl:apply-templates select="eaxs:Sender"/>
											</pdfmailmeta:Sender>
										</xsl:if>
										<xsl:if test="eaxs:OrigDate">
											<pdfmailmeta:Sent><xsl:value-of select="eaxs:OrigDate"/></pdfmailmeta:Sent>
										</xsl:if>
										<xsl:if test="eaxs:Subject">
											<pdfmailmeta:Subject>
												<xsl:value-of select="eaxs:Subject"/>
											</pdfmailmeta:Subject>							
										</xsl:if>
										<xsl:if test="eaxs:Keywords">
											<pdfmailmeta:Keywords>
												<rdf:Bag>
													<xsl:apply-templates select="eaxs:Keywords"/>
												</rdf:Bag>
											</pdfmailmeta:Keywords>							
										</xsl:if>
										<xsl:if test="eaxs:Comments">
											<pdfmailmeta:Comments>
												<rdf:Bag>
													<xsl:apply-templates select="eaxs:Comments"/>
												</rdf:Bag>
											</pdfmailmeta:Comments>
										</xsl:if>
										<pdfmailmeta:Message-ID><xsl:value-of select="eaxs:MessageId"/></pdfmailmeta:Message-ID>							
										<pdfmailmeta:GUID><xsl:value-of select="eaxs:Guid"/></pdfmailmeta:GUID>							
										<xsl:if test="eaxs:MessageProperties/eaxs:Size">
											<pdfmailmeta:SizeInBytes><xsl:value-of select="eaxs:MessageProperties/eaxs:Size"/></pdfmailmeta:SizeInBytes>							
										</xsl:if>
										<pdfmailmeta:NumberAttachments><xsl:value-of select="count(.//eaxs:*[@IsAttachment='true'])"/></pdfmailmeta:NumberAttachments>							
										<xsl:if test="eaxs:Cc">
											<pdfmailmeta:Cc>
												<rdf:Seq>
													<xsl:for-each select="eaxs:Cc/*">
														<rdf:li>
															<xsl:apply-templates select="."/>
														</rdf:li>
													</xsl:for-each>
												</rdf:Seq>
											</pdfmailmeta:Cc>
										</xsl:if>
										<xsl:if test="eaxs:Bcc">
											<pdfmailmeta:Bcc>
												<rdf:Seq>
													<xsl:for-each select="eaxs:Bcc/*">
														<rdf:li>
															<xsl:apply-templates select="."/>
														</rdf:li>
													</xsl:for-each>
												</rdf:Seq>
											</pdfmailmeta:Bcc>
										</xsl:if>
										<xsl:if test="eaxs:InReplyTo">
											<pdfmailmeta:In-Reply-To>
												<rdf:Seq>
													<xsl:apply-templates select="eaxs:InReplyTo"/>
												</rdf:Seq>
											</pdfmailmeta:In-Reply-To>
										</xsl:if>
										<xsl:if test="eaxs:References">
											<pdfmailmeta:References>
												<rdf:Seq>
													<xsl:apply-templates select="eaxs:References"/>
												</rdf:Seq>
											</pdfmailmeta:References>							
										</xsl:if>
									</pdfmailmeta:Email>
								</rdf:li>
							</xsl:for-each>
						</rdf:Seq>
					</pdfmailmeta:email>

					<pdfmailmeta:attachments>
						<rdf:Seq>
							<xsl:for-each select="//eaxs:SingleBody/eaxs:ExtBodyContent | //eaxs:SingleBody/eaxs:BodyContent[fn:lower-case(normalize-space(../@IsAttachment)) = 'true' or not(starts-with(fn:lower-case(normalize-space(../eaxs:ContentType)),'text/'))]">
								<rdf:li>
									<pdfmailmeta:Attachment>
										<!-- The 'urn:hash:' is based on draft https://datatracker.ietf.org/doc/html/draft-thiemann-hash-urn-01 -->
										<xsl:attribute name="rdf:about">urn:hash:<xsl:value-of select="fn:lower-case(eaxs:Hash/eaxs:Function)"/>:<xsl:value-of select="fn:lower-case(eaxs:Hash/eaxs:Value)"/></xsl:attribute>
										<xsl:choose>
											<xsl:when test="../eaxs:DispositionFileName | ../eaxs:ContentName">
												<pdfmailmeta:Filename>
													<xsl:value-of select="(../eaxs:DispositionFileName | ../eaxs:ContentName)[1]"/>
												</pdfmailmeta:Filename>
											</xsl:when>
											<xsl:otherwise>
												<pdfmailmeta:Filename>*No filename was given*</pdfmailmeta:Filename>
											</xsl:otherwise>
										</xsl:choose>
										<pdfmailmeta:SizeInBytes><xsl:value-of select="eaxs:Size"/></pdfmailmeta:SizeInBytes>
										<pdfmailmeta:Email>
											<xsl:attribute name="rdf:resource">mid:<xsl:value-of select="fn:encode-for-uri(ancestor::*[eaxs:MessageId][1]/eaxs:MessageId)"/></xsl:attribute>
										</pdfmailmeta:Email>
										<pdfmailmeta:Message-ID><xsl:value-of select="ancestor::*[eaxs:MessageId][1]/eaxs:MessageId"/></pdfmailmeta:Message-ID>
										<pdfmailmeta:GUID><xsl:value-of select="ancestor::*[eaxs:MessageId][1]/eaxs:Guid"/></pdfmailmeta:GUID>
										<pdfmailmeta:Content-Type>
											<xsl:call-template name="FullContentTypeHeader"/>
										</pdfmailmeta:Content-Type>
										<xsl:choose>
											<xsl:when test="fn:upper-case(normalize-space(eaxs:Hash/eaxs:Function))='MD5'">
												<pdfmailmeta:CheckSum><xsl:value-of select="fn:lower-case(normalize-space(eaxs:Hash/eaxs:Value))"/></pdfmailmeta:CheckSum>
											</xsl:when>
											<xsl:otherwise>
												<xsl:message terminate="no">WARNING: Unsupported hash algorithm '<xsl:value-of select="eaxs:Hash/eaxs:Function"/>'</xsl:message>
											</xsl:otherwise>
										</xsl:choose>
									</pdfmailmeta:Attachment>
								</rdf:li>
							</xsl:for-each>
						</rdf:Seq>
					</pdfmailmeta:attachments>
					
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
