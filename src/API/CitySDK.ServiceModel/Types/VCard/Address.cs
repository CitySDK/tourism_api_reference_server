using System;
using System.Text;

namespace CitySDK.ServiceModel.Types.VCard
{
  public class Address
  {
    #region private properties

    private string m_postOfficeAddress;

    private string m_extendedAddress;

    private string m_street;

    private string m_locality;

    private string m_region;

    private string m_postalCode;

    private string m_country;

    #endregion

    #region Public properties

    public string PostOfficeAddress
    {
      get { return m_postOfficeAddress; }
      set { m_postOfficeAddress = value; }
    }

    public string ExtendedAddress
    {
      get { return m_extendedAddress; }
      set { m_extendedAddress = value; }
    }

    public string Street
    {
      get { return m_street; }
      set { m_street = value; }
    }

    public string Locality
    {
      get { return m_locality; }
      set { m_locality = value; }
    }

    public string Region
    {
      get { return m_region; }
      set { m_region = value; }
    }

    public string PostalCode
    {
      get { return m_postalCode; }
      set { m_postalCode = value; }
    }

    public string Country
    {
      get { return m_country; }
      set { m_country = value; }
    }

    #endregion

    public AddressType DeliveryAddressType;

    public Address()
    {

    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();

      sb.Append(string.Format("ADR;{0}:", Enum.GetName(typeof(AddressType), DeliveryAddressType)));
      sb.Append(string.Format("{0};", m_postOfficeAddress));
      sb.Append(string.Format("{0};", m_extendedAddress));
      sb.Append(string.Format("{0};", m_street));
      sb.Append(string.Format("{0};", m_locality));
      sb.Append(string.Format("{0};", m_region));
      sb.Append(string.Format("{0};", m_postalCode));
      sb.Append(string.Format("{0}\r\n", m_country));

      return sb.ToString();
    }
  }
}