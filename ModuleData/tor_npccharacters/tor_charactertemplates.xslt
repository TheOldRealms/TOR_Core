<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output omit-xml-declaration="yes"/>
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()" />
        </xsl:copy>
    </xsl:template>
	
    <xsl:template match="NPCCharacter[@occupation='Wanderer']" />
  <xsl:template match="NPCCharacter[@id = 'gear_practice_dummy_empire' or 
@id = 'weapon_practice_stage_1_empire' or 
@id = 'weapon_practice_stage_2_empire' or 
@id = 'weapon_practice_stage_3_empire']"/>
</xsl:stylesheet>
