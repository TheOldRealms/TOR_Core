<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output omit-xml-declaration="yes"/>
	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>
	<xsl:template name="string-replace-all">
		<xsl:param name="text" />
		<xsl:param name="replace" />
		<xsl:param name="by" />
		<xsl:choose>
			<xsl:when test="contains($text, $replace)">
				<xsl:value-of select="substring-before($text,$replace)" />
				<xsl:value-of select="$by" />
				<xsl:call-template name="string-replace-all">
					<xsl:with-param name="text" select="substring-after($text,$replace)" />
					<xsl:with-param name="replace" select="$replace" />
					<xsl:with-param name="by" select="$by" />
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$text" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template match="NPCCharacter/@name[contains(.,'Vlandia')]">
		<xsl:variable name="replace_bretonnia">
			<xsl:call-template name="string-replace-all">
				<xsl:with-param name="text" select="." />
				<xsl:with-param name="replace" select="'Vlandia'" />
				<xsl:with-param name="by" select="''" />
			</xsl:call-template>
		</xsl:variable>
		<xsl:variable name="replace_prefix">
			<xsl:call-template name="string-replace-all">
				<xsl:with-param name="text" select="$replace_bretonnia" />
				<xsl:with-param name="replace" select="substring-before(substring-after($replace_bretonnia, '{'), '}')" />
				<xsl:with-param name="by" select="'=!'" />
			</xsl:call-template>
		</xsl:variable>
		<xsl:attribute name="name">
			<xsl:value-of select="$replace_prefix" />
		</xsl:attribute>
	</xsl:template>
</xsl:stylesheet>