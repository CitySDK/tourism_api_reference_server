﻿using System;
using System.Text;

namespace CitySDK.ServiceModel.Types.VCard
{
  public class Telephone
  {
    private string m_number;

    public string Number
    {
      get { return m_number; }
      set { m_number = value; }
    }

    public NumberType NumberType;

    public Telephone()
    {

    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();

      sb.Append(string.Format("TEL;{0}:", Enum.GetName(typeof(NumberType), NumberType)));
      sb.Append(string.Format("{0}\r\n", m_number));

      return sb.ToString();
    }
  }
}