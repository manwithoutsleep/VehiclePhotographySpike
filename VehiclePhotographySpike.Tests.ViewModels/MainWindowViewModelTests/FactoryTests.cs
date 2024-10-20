namespace VehiclePhotographySpike.Tests.ViewModels.MainWindowViewModelTests
{
    [TestClass]
    public class FactoryTests
    {
        [TestMethod]
        public void Default_ReturnAnEmptyListOfFolders()
        {
            // Arrange
            var expected = new List<string>();

            // Act
            var actual = VehiclePhotographySpike.ViewModels.ViewModels.MainWindowViewModel.Create();

            // Assert
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
