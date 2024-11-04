using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace SRegulV2
{
    public partial class FInfoAssure : Form
    {

        private string NumCarte = "";
        private string Nom = "", Prenom = "", DateNaissance = "", AVS = "", Genre = "";
        private string Adresse = "", NomAssurance = "", TypeAssurance = "", NumAssure = "", EAN = "", DateEcheance = "";

        public FInfoAssure(string NCarte)
        {
            InitializeComponent();

            NumCarte = NCarte;
            AppelService();
            bExit.Focus();
        }

        private void AppelService()
        {
            var url = "https://194.230.88.180:7080/assurpatient/RecupDataAssurance.asmx/RecupedataFromCoverCard?numcard=" + NumCarte;

            //Pour passer le blocage du certificat autosigné
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            /*ServicePointManager.ServerCertificateValidationCallback += delegate
            {
                return true;
            };*/

            try
            {
                //appel du webservice
                HttpWebRequest requete = (HttpWebRequest)WebRequest.Create(url);
                requete.ContentType = "text/xml";

                //On met des ressources raisonnable pour cette requette
                requete.MaximumAutomaticRedirections = 4;
                requete.MaximumResponseHeadersLength = 4;

                //Récuperation de la réponse
                HttpWebResponse reponse = (HttpWebResponse)requete.GetResponse();

                //On le met dans un stream
                Stream responseStream = reponse.GetResponseStream();

                Console.WriteLine(responseStream);

                //recup de la reponse
                XmlTextReader XMLreader = new XmlTextReader(responseStream);

                rTBoxInfos.Text = "";    //on vide le champs text

                //C'est un datasetXML. Ici il nous renvoie une seule chaine texte avec tout dedans
                //on va récupérer cette unique chaine et la mettre dans un doc xml pour pouvoir la parser
                while (XMLreader.Read())
                {
                    //On ne fait rien si c'est blancs ou la fin
                    if (XMLreader.NodeType == XmlNodeType.Whitespace || XMLreader.NodeType == XmlNodeType.EndElement) continue;

                    //Si on du texte....c'est celui qu'on attend
                    if (XMLreader.NodeType == XmlNodeType.Text)
                    {
                        //On le charge dans un doc xml
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(XMLreader.Value.Trim());    //On enleve les blancs

                        XmlNodeList nodes = doc.DocumentElement.SelectNodes("/DatasetCard/DataCard");

                        //Puis on parse
                        foreach (XmlNode node in nodes)
                        {
                            Nom = node.SelectSingleNode("nom").InnerText;
                            Prenom = node.SelectSingleNode("prenom").InnerText;

                            //traitement de la date de naissance
                            DateTime dateNaiss;
                            if (DateTime.TryParse(node.SelectSingleNode("DateNaissance").InnerText, out dateNaiss))
                            {
                                DateNaissance = dateNaiss.ToString("dd.MM.yyyy");
                            }
                            else DateNaissance = "";

                            AVS = node.SelectSingleNode("AVS").InnerText;
                            Genre = node.SelectSingleNode("Genre").InnerText;
                            Adresse = node.SelectSingleNode("Adresse").InnerText;

                            NomAssurance = node.SelectSingleNode("NomAssurance").InnerText;
                            TypeAssurance = node.SelectSingleNode("TypeAssurance").InnerText;
                            NumAssure = node.SelectSingleNode("NuméroAssuré").InnerText;
                            EAN = node.SelectSingleNode("EAN-Numbre").InnerText;

                            //traitement de la date d'échéance
                            DateTime echeance;
                            if (DateTime.TryParse(node.SelectSingleNode("DateEchéance").InnerText, out echeance))
                            {
                                DateEcheance = echeance.ToString("dd.MM.yyyy");
                            }
                            else DateEcheance = "";

                            Console.WriteLine(Nom + " " + Prenom + " " + DateNaissance);

                            //On ecrit le tout dans le textlist
                            string nvlleligne = Environment.NewLine;

                            rTBoxInfos.Text += "Nom : " + Nom + nvlleligne + "Prénom : " + Prenom + nvlleligne + "Date de naissance : " + DateNaissance + nvlleligne
                                           + "N° AVS : " + AVS + nvlleligne + "Genre : " + Genre + nvlleligne
                                           + "Adresse : " + Adresse + nvlleligne + nvlleligne + "Nom de l'assurance : " + NomAssurance + nvlleligne + "Type d'assurance : " + TypeAssurance
                                           + nvlleligne + "N° d'assuré : " + NumAssure + nvlleligne
                                           + "EAN de l'assurance : " + EAN + nvlleligne + "Date d'échéance : " + DateEcheance;
                            //RetourInfos += Nom + Prenom + DateNaissance + AVS + Genre + Adresse + NomAssurance
                            // + TypeAssurance + NumAssure + EAN + DateEcheance;
                        }
                    }
                }

                //Fermeture de la réponse
                reponse.Close();

                //Si on a reçu des infos, on active le bouton exporter
                if (Nom != "")
                    bExporter.Enabled = true;
                else    //sinon on signal qu'on a rien trouvé
                {
                    bExporter.Enabled = false;
                    rTBoxInfos.Text = "aucune donnée n'a été trouvée";
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur " + ex.Message);
            }
        }

        private void bExporter_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Attention vous allez remplacer l'adresse actuelle de la fiche\r\n" +
                                        "par l'adresse figurant sur la carte d'assuré.\r\n" +
                                        "Est-ce bien ce que vous loulez faire?.", "Attention remplacement de l'adresse!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            //Création d'un doublon!
            if (dialogResult == DialogResult.Yes)
            {
                //Export des données dans la fiche Visite     
                FVisite.NumAssure = NumAssure;
                FVisite.NomAssure = Nom;
                FVisite.PrenomAssure = Prenom;
                FVisite.DateNaissanceAssure = DateNaissance;
                FVisite.GenreAssure = Genre;

                //Puis on décompose l'adresse
                string[] AdresseOK = DecomposeAdresse(Adresse);

                FVisite.AdresseDecompose = AdresseOK;
            }

            //Puis on ferme la forme
            Close();
        }

        private string[] DecomposeAdresse(string AdresseAdecomposer)
        {
            string[] Adresse = new string[5];
            string Rue = "";
            string NumRue = "";
            string CP = "";
            string Commune = "";
            string Pays = "";

            for (int i = 0; i < 5; i++)
            {
                Adresse[i] = "";
            }

            try
            {
                Regex re = new Regex(@"\d+");
                Match m = re.Match(AdresseAdecomposer);
                if (m.Success)
                {
                    Rue = AdresseAdecomposer.Substring(0, m.Index).Trim();          //string.Format("RegEx found " + m.Value + " at position " + m.Index.ToString());
                    NumRue = m.Value.ToString();

                    //Puis pour le code postal
                    AdresseAdecomposer = AdresseAdecomposer.Substring(m.Index + m.Length, AdresseAdecomposer.Length - (m.Index + m.Length));

                    m = re.Match(AdresseAdecomposer);
                    Commune = AdresseAdecomposer.Substring(0, m.Index).Trim();

                    CP = m.Value.ToString();

                    //Puis pour le pays
                    AdresseAdecomposer = AdresseAdecomposer.Substring(m.Index + m.Length, AdresseAdecomposer.Length - (m.Index + m.Length));

                    Pays = AdresseAdecomposer.TrimStart();

                    if (Pays == "FR")
                        Pays = "France";

                    Adresse[0] = Rue;
                    Adresse[1] = NumRue;
                    Adresse[2] = Commune;
                    Adresse[3] = CP;
                    Adresse[4] = Pays;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Echec de l'export de l'adresse du patient", "Exportation des données du patient", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(e);
            }

            return Adresse;
        }


        private void bExit_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
