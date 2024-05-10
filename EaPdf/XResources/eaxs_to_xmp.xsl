<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="2.0" 
	xmlns:eaxs="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2"

	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:xs="http://www.w3.org/2001/XMLSchema"
	xmlns:fn="http://www.w3.org/2005/xpath-functions"
	
	xmlns:foaf="http://xmlns.com/foaf/0.1/"
	xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"

    exclude-result-prefixes="eaxs xsl xs fn"
	>

	<xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes" omit-xml-declaration="no" />
	
	<xsl:template match="/">
		<root>
			<xsl:apply-templates select="/eaxs:Account/eaxs:Folder[eaxs:Message or eaxs:Folder]"/>
		</root>
	</xsl:template>

	<xsl:template match="eaxs:Folder">
		<folder>
			<xsl:attribute name="Name"><xsl:value-of select="eaxs:Name"/></xsl:attribute>
			<xsl:apply-templates select="eaxs:Message"/>
			<xsl:apply-templates select="eaxs:Folder[eaxs:Message or eaxs:Folder]"/>
		</folder>
	</xsl:template>
	
	<xsl:template match="eaxs:Message">
		<message>
			<xsl:attribute name="NamedDestination">MESSAGE_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute>
			<xsl:attribute name="NamedDestinationEnd">MESSAGE_END_<xsl:value-of select="eaxs:LocalId"/></xsl:attribute>
			<xsl:attribute name="LocalId"><xsl:value-of select="eaxs:LocalId"/></xsl:attribute>
			<xsl:attribute name="MessageId"><xsl:value-of select="eaxs:MessageId"/></xsl:attribute>
			<xsl:processing-instruction name="xpacket">begin="" id="W5M0MpCehiHzreSzNTczkc9d" </xsl:processing-instruction>
			<x:xmpmeta xmlns:x="adobe:ns:meta/">
				<rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
					<rdf:Description rdf:about=""
						xmlns:dc="http://purl.org/dc/elements/1.1/"
						xmlns:dcterms="http://purl.org/dc/terms/"
						xmlns:foaf="http://xmlns.com/foaf/0.1/"
						xmlns:rfc5322="http://www.pdfa.org/eapdf/ns/rfc5322/"
						>
						
						<dc:identifier><xsl:value-of select="eaxs:MessageId"/></dc:identifier>
						
						<!--
						<xsl:if test="eaxs:Subject">
							<dc:subject>
								<rdf:Bag>
									<rdf:li>
										<xsl:value-of select="eaxs:Subject"/>
									</rdf:li>
									<xsl:apply-templates select="eaxs:Keywords"/>
								</rdf:Bag>
							</dc:subject>							
						</xsl:if>

						<xsl:if test="eaxs:Comments">
							<dc:description>
								<rdf:Alt>
									<xsl:apply-templates select="eaxs:Comments"/>
								</rdf:Alt>
							</dc:description>
						</xsl:if>

						<xsl:if test="eaxs:OrigDate">
							<dcterms:created><xsl:value-of select="eaxs:OrigDate"/></dcterms:created>
						</xsl:if>
						
						<xsl:if test="eaxs:Sender">
							<rfc5322:sender>
								<xsl:apply-templates select="eaxs:Sender"/>
							</rfc5322:sender>
						</xsl:if>
						
						<xsl:if test="eaxs:From">
							<rfc5322:from>
								<rdf:Seq>
									<xsl:for-each select="eaxs:From/*">
										<rdf:li>
											<xsl:apply-templates select="."/>
										</rdf:li>
									</xsl:for-each>
								</rdf:Seq>
							</rfc5322:from>							
						</xsl:if>
						
						<xsl:if test="eaxs:To">
							<rfc5322:to>
								<rdf:Seq>
									<xsl:for-each select="eaxs:To/*">
										<rdf:li>
											<xsl:apply-templates select="."/>
										</rdf:li>
									</xsl:for-each>
								</rdf:Seq>
							</rfc5322:to>
						</xsl:if>
						
						<xsl:if test="eaxs:Cc">
							<rfc5322:cc>
								<rdf:Seq>
									<xsl:for-each select="eaxs:Cc/*">
										<rdf:li>
											<xsl:apply-templates select="."/>
										</rdf:li>
									</xsl:for-each>
								</rdf:Seq>
							</rfc5322:cc>
						</xsl:if>
						
						<xsl:if test="eaxs:Bcc">
							<rfc5322:bcc>
								<rdf:Seq>
									<xsl:for-each select="eaxs:Bcc/*">
										<rdf:li>
											<xsl:apply-templates select="."/>
										</rdf:li>
									</xsl:for-each>
								</rdf:Seq>
							</rfc5322:bcc>
						</xsl:if>
						
						<xsl:if test="eaxs:InReplyTo">
							<rfc5322:inReplyTo>
								<rdf:Seq>
									<xsl:apply-templates select="eaxs:InReplyTo"/>
								</rdf:Seq>
							</rfc5322:inReplyTo>
						</xsl:if>
						<xsl:if test="eaxs:References">
							<dcterms:references>
								<rdf:Seq>
									<xsl:apply-templates select="eaxs:References"/>
								</rdf:Seq>
							</dcterms:references>							
						</xsl:if>
						-->
						
					</rdf:Description>
				</rdf:RDF>
			</x:xmpmeta>
			<xsl:processing-instruction name="xpacket">end="r"</xsl:processing-instruction>
		</message>
	</xsl:template>

	<xsl:template match="eaxs:Mailbox | eaxs:Sender">
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
	
	<xsl:template match="eaxs:Group" >
		<foaf:Group>
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
		</foaf:Group>
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
		<rdf:li xml:lang="x-default"><xsl:value-of select="."/></rdf:li>
	</xsl:template>
	
</xsl:stylesheet>
