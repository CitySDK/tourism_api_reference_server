using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CitySDK.ServiceModel.Types.VCard
{
    public class VCard
    {
        #region Private properties

        private string m_formattedName;
        private string m_lastName;
        private string m_firstName;
        private string m_middleName;
        private string m_namePrefix;
        private string m_nameSuffix;
        private DateTime m_birthDate = DateTime.MinValue;

        private IEnumerable<Address> m_addresses = new List<Address>();

        public IEnumerable<Address> Addresses
        {
            get { return m_addresses; }
            set { m_addresses = value; }
        }

        private IEnumerable<Telephone> m_phoneNumber = new List<Telephone>();

        public IEnumerable<Telephone> PhoneNumber
        {
            get { return m_phoneNumber; }
            set { m_phoneNumber = value; }
        }

        private IEnumerable<Fax> m_faxNumber = new List<Fax>();

        public IEnumerable<Fax> FaxNumber
        {
            get { return m_faxNumber; }
            set { m_faxNumber = value; }
        }

        private IEnumerable<string> m_emailAddresses = new List<string>();

        private IEnumerable<string> m_url = new List<string>();

        public IEnumerable<string> Url
        {
            get { return m_url; }
            set { m_url = value; }
        }

        private string m_title;
        private string m_role;
        private string m_organization;

        #endregion

        public VCard()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        #region Public Properties

        public string FormattedName
        {
            get { return m_formattedName; }
            set { m_formattedName = value; }
        }

        public string LastName
        {
            get { return m_lastName; }
            set { m_lastName = value; }
        }

        public string FirstName
        {
            get { return m_firstName; }
            set { m_firstName = value; }
        }

        public string MiddleName
        {
            get { return m_middleName; }
            set { m_middleName = value; }
        }

        public string NamePrefix
        {
            get { return m_namePrefix; }
            set { m_namePrefix = value; }
        }

        public string NameSuffix
        {
            get { return m_nameSuffix; }
            set { m_nameSuffix = value; }
        }

        public DateTime BirthDate
        {
            get { return m_birthDate; }
            set { m_birthDate = value; }
        }

        public IEnumerable<string> EmailAddresses
        {
            get { return m_emailAddresses; }
            set { m_emailAddresses = value; }
        }

        public string Title
        {
            get { return m_title; }
            set { m_title = value; }
        }

        public string Role
        {
            get { return m_role; }
            set { m_role = value; }
        }

        public string Organization
        {
            get { return m_organization; }
            set { m_organization = value; }
        }

        #endregion

        #region Public Methods

        public void Generate(string filePath, FileMode mode)
        {
            using (FileStream fs = new FileStream(filePath, mode))
            {
                Generate(fs);
            }
        }

        public void Generate(Stream outputStream)
        {
            using (StreamWriter sw = new StreamWriter(outputStream))
            {
                sw.Write(this.ToString());
            }
        }

        #endregion

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("BEGIN:VCARD\r\n");
            sb.Append("VERSION:2.1\r\n");

            //Add Formatted name
            if (m_formattedName != null)
              sb.Append("FN:" + m_formattedName + "\r\n");
            //else
            //  sb.Append("FN:" + m_namePrefix + " " + m_firstName + " " + m_middleName + " " + m_lastName + " " + m_nameSuffix + "\r\n");

            //Add the name
            sb.Append(string.Format("N:{0};", m_lastName));
            sb.Append(string.Format("{0};", m_firstName));
            sb.Append(string.Format("{0};", m_middleName));
            sb.Append(string.Format("{0};", m_namePrefix));
            sb.Append(string.Format("{0};", m_nameSuffix));
            sb.Append("\r\n");
            //Add a birthday
            if (m_birthDate != DateTime.MinValue)
            {
                sb.Append(string.Format("BDAY:{0}\r\n", m_birthDate.ToString("yyyyMMdd")));
            }

            //Add Delivery Addresses
            foreach (Address da in m_addresses)
            {
                sb.Append(da.ToString());
            }

            //Add phone numbers
            foreach (Telephone phone in m_phoneNumber)
            {
                sb.Append(phone.ToString());
            }

            //Add email address
            foreach (string email in m_emailAddresses)
            {
                sb.Append(string.Format("EMAIL;INTERNET:{0}\r\n", email));
            }

            //Add url address
            foreach (string url in m_url)
            {
                sb.Append(string.Format("URL;WORK:{0}\r\n", url));
            }

            //Add Title
            if(!string.IsNullOrEmpty(m_title))
              sb.Append(string.Format("TITLE:{0}\r\n", m_title));

            //Business Category
            if(!string.IsNullOrEmpty(m_role))
            sb.Append(string.Format("ROLE:{0}\r\n", m_role));

            //Organization
            if(!string.IsNullOrEmpty(m_organization))
              sb.Append(string.Format("ORG:{0}\r\n", m_organization));

            sb.Append("END:VCARD\r\n");
            return sb.ToString();
        }
    }
}