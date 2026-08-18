using ANLAbel.Core.Printing;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class DocumentPrinterIdentityContractTests
{
    [Fact]
    public void PreferencesNeverInventAQueue()
    {
        Assert.Null(DocumentPrinterIdentityContract.QueueNameFromDocument(null, "HP LaserJet"));
        Assert.Null(DocumentPrinterIdentityContract.QueueNameFromDocument("   ", "Windows Default"));
        Assert.Null(DocumentPrinterIdentityContract.QueueNameFromDocument(null, null));
    }

    [Fact]
    public void DocumentQueueWinsOverPreferences()
    {
        Assert.Equal(
            "Zebra ZT411",
            DocumentPrinterIdentityContract.QueueNameFromDocument("Zebra ZT411", "HP LaserJet"));
        Assert.Equal(
            "Zebra ZT411",
            DocumentPrinterIdentityContract.QueueNameFromDocument("  Zebra ZT411  ", "Windows Default"));
    }

    [Fact]
    public void PaperHintIsUsedOnlyWhenTheDocumentHasNoPaper()
    {
        Assert.Equal(
            "50 × 20 mm",
            DocumentPrinterIdentityContract.PaperNameFromDocumentOrHint("50 × 20 mm", "100 × 150 mm shipping"));
        Assert.Equal(
            "100 × 150 mm shipping",
            DocumentPrinterIdentityContract.PaperNameFromDocumentOrHint(null, "100 × 150 mm shipping"));
        Assert.Null(DocumentPrinterIdentityContract.PaperNameFromDocumentOrHint("  ", "  "));
    }
}
