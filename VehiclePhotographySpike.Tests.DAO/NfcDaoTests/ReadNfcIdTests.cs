using VehiclePhotographySpike.Dao;

namespace VehiclePhotographySpike.Tests.DAO.NfcDaoTests
{
    [TestClass]
    public class ReadNfcIdTests
    {
        [TestMethod]
        public void ReadNfcId()
        {
            var nfcDao = new NfcDao();
            var nfcId = nfcDao.ReadTagId();
            Assert.IsNotNull(nfcId);
        }
    }
}
