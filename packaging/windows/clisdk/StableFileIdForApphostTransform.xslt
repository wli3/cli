<xsl:stylesheet version="1.0"
            xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
            xmlns:msxsl="urn:schemas-microsoft-com:xslt"
            exclude-result-prefixes="msxsl"
            xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
            xmlns:my="my:my">

    <xsl:output method="xml" indent="yes" />

    <xsl:template match="*[contains(name(), 'AppHostTemplate\apphost.exe')]">
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:attribute name="Id">
                <xsl:text>apphosttemplateapphostexe</xsl:text>
            </xsl:attribute>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>
