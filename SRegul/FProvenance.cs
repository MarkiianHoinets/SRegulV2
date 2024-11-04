﻿using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SRegulV2
{
    public partial class FProvenance : Form
    {
        public static DataTable dtProvenance = new DataTable();
        private static string Etat = "Lecture";        

        public FProvenance()
        {
            InitializeComponent();

            //Def des colonnes du Listeview
            listView1.Columns.Add("IdProvenance", 1);         //Colonne invisible
            listView1.Columns.Add("Libelle", 200);
            listView1.Columns.Add("Code Provenance", 100);
            listView1.View = View.Details;    //Pour afficher les subItems  

            //On vide les champs et iniitalise les boutons
            VideChamps();
        }

        private void FProvenance_Load(object sender, EventArgs e)
        {
            dtProvenance.Clear();
            dtProvenance = FonctionsAppels.ChargeListeProvenance();

            //On vide la liste pour la rafraichir                
            listView1.BeginUpdate();
            listView1.Items.Clear();

            for (int i = 0; i < dtProvenance.Rows.Count; i++)
            {
                ListViewItem item = new ListViewItem(dtProvenance.Rows[i]["IdProvenance"].ToString());
                item.SubItems.Add(dtProvenance.Rows[i]["LibelleProvenance"].ToString());
                item.SubItems.Add(dtProvenance.Rows[i]["CodeProvenance"].ToString());

                listView1.Items.Add(item);
            }

            listView1.EndUpdate();  //Rafraichi le controle        
        }

        private void VideChamps()
        {
            lIdProvenance.Text = "";
            tBProvenance.Text = "";
            tBCodeProvenance.Text = "";

            //Pour les boutons
            bAjouter.ImageIndex = 0;
            bAjouter.Visible = true;

            bValider.ImageIndex = 4;
            bValider.Enabled = false;

            bSupprimer.ImageIndex = 6;
            bSupprimer.Enabled = false;

            bCancel.Visible = false;

            bExit.ImageIndex = 7;
        }

        //Selection d'une provenance dans la liste
        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            int index = listView1.SelectedItems[0].Index;

            //On peuple les champs
            lIdProvenance.Text = dtProvenance.Rows[index]["IdProvenance"].ToString();
            tBProvenance.Text = dtProvenance.Rows[index]["LibelleProvenance"].ToString();
            tBCodeProvenance.Text = dtProvenance.Rows[index]["CodeProvenance"].ToString();

            //Gestion des boutons: On passe en modif
            Etat = "Modif";

            bAjouter.Visible = false;
            bValider.Enabled = true;
            bCancel.Visible = true;
            bSupprimer.Enabled = true;
        }

        //***Gestion des boutons
        private void bAjouter_Click(object sender, EventArgs e)
        {
            //Gestion des boutons: On passe en Ajout
            Etat = "Ajout";

            VideChamps();

            bAjouter.Visible = false;
            bValider.Enabled = true;
            bCancel.Visible = true;
            bSupprimer.Enabled = false;
        }

        //On valide en fonction de l'état
        private void bValider_Click(object sender, EventArgs e)
        {
            if (VerifSaisie() == "OK")
            {
                if (Etat == "Ajout")
                {
                    if (AjoutEnreg() == "OK")
                    {
                        VideChamps();

                        //Gestion des boutons
                        bAjouter.Visible = true;
                        bValider.Enabled = false;
                        bCancel.Visible = false;
                        bSupprimer.Enabled = false;

                        Etat = "Lecture";

                        //On rafraichi la liste des provenances
                        FProvenance_Load(sender, e);
                    }
                    else
                    {
                        //Gestion des boutons
                        bAjouter.Visible = false;
                        bValider.Enabled = true;
                        bCancel.Visible = true;
                        bSupprimer.Enabled = false;
                    }
                }
                else if (Etat == "Modif")
                {
                    if (UpdateEnreg() == "OK")
                    {
                        VideChamps();
                        //Gestion des boutons
                        bAjouter.Visible = true;
                        bValider.Enabled = false;
                        bCancel.Visible = false;
                        bSupprimer.Enabled = false;

                        Etat = "Lecture";

                        //On rafraichi la liste des provenances
                        FProvenance_Load(sender, e);
                    }
                    else
                    {
                        //Gestion des boutons
                        bAjouter.Visible = false;
                        bValider.Enabled = true;
                        bCancel.Visible = true;
                        bSupprimer.Enabled = false;
                    }
                }
            }
        }


        //vérification des saisies
        private string VerifSaisie()
        {
            string retour = "KO";
            int V1 = 0;

            if (tBProvenance.Text != "")
                V1 += 1;
            else
                MessageBox.Show("Vous devez renseigner le nom de la provenance. ", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (tBCodeProvenance.Text != "")
                V1 += 1;
            else
                MessageBox.Show("Vous devez renseigner le code provenance. ", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);

            //En fonction des résultats
            switch (V1)
            {
                case 2: retour = "OK"; break;
                default: retour = "KO"; break;
            }

            return retour;
        }


        //On ajoute un nvl enregistrement 
        public string AjoutEnreg()
        {
            string Retour = "KO";

            //Chaine de connection... ici on attaque MariaDB
            string connex = ConfigurationManager.ConnectionStrings["Connection_Base_Regul"].ToString();
            MySqlConnection dbConnection = new MySqlConnection(connex);

            dbConnection.Open();
            MySqlCommand cmd = new MySqlCommand();

            cmd.Connection = dbConnection;

            //On déclare ici la requete (à cause de la transaction)
            string SqlStr0 = "";

            try
            {
                //Puis dans une transaction
                MySqlTransaction trans;

                //Def de la requete
                //*****Provenance***
                SqlStr0 = "INSERT INTO provenance ";
                SqlStr0 += " (LibelleProvenance, CodeProvenance)";
                SqlStr0 += " VALUES('" + tBProvenance.Text + "','" + tBCodeProvenance.Text + "')";

                //Ouverture d'une transaction
                trans = dbConnection.BeginTransaction();
                try
                {
                    //on execute les requettes                                       
                    cmd.CommandText = SqlStr0; cmd.Transaction = trans; cmd.ExecuteNonQuery();

                    //on valide la transaction
                    trans.Commit();

                    Retour = "OK";
                }
                catch (Exception e)
                {
                    trans.Rollback();
                    MessageBox.Show("Erreur lors de la création de la provenance (transaction). " + e.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    //fermeture des connexions
                    if (dbConnection.State == System.Data.ConnectionState.Open)
                    {
                        dbConnection.Close();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Erreur lors de la création de la provenance. " + e.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
            finally
            {
                //fermeture des connexions
                if (dbConnection.State == System.Data.ConnectionState.Open)
                {
                    dbConnection.Close();
                }
            }

            return Retour;
        }


        //On met à jour un enregistrement
        private string UpdateEnreg()
        {
            string Retour = "KO";

            //Chaine de connection... ici on attaque MariaDB
            string connex = ConfigurationManager.ConnectionStrings["Connection_Base_Regul"].ToString();
            MySqlConnection dbConnection = new MySqlConnection(connex);

            dbConnection.Open();
            MySqlCommand cmd = new MySqlCommand();

            cmd.Connection = dbConnection;

            //On déclare ici la requete (à cause de la transaction)
            string SqlStr0 = "";

            try
            {
                //Puis dans une transaction
                MySqlTransaction trans;

                //Def de la requete
                //*****Commune***
                SqlStr0 = "UPDATE provenance ";
                SqlStr0 += " SET LibelleProvenance = '" + tBProvenance.Text + "', CodeProvenance = '" + tBCodeProvenance.Text + "'";
                SqlStr0 += " WHERE IdProvenance = '" + lIdProvenance.Text + "'";

                //Ouverture d'une transaction
                trans = dbConnection.BeginTransaction();
                try
                {
                    //on execute les requettes                                       
                    cmd.CommandText = SqlStr0; cmd.Transaction = trans; cmd.ExecuteNonQuery();

                    //on valide la transaction
                    trans.Commit();
                    Retour = "OK";
                }
                catch (Exception e)
                {
                    trans.Rollback();
                    MessageBox.Show("Erreur lors de la modification de la provenance (transaction). " + e.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    //fermeture des connexions
                    if (dbConnection.State == System.Data.ConnectionState.Open)
                    {
                        dbConnection.Close();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Erreur lors de la modification de la provenance. " + e.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
            finally
            {
                //fermeture des connexions
                if (dbConnection.State == System.Data.ConnectionState.Open)
                {
                    dbConnection.Close();
                }
            }

            return Retour;
        }

        private void bSupprimer_Click(object sender, EventArgs e)
        {
            //On essaie de supprimer la Provenance...S'il elle n'a jamais été utilisée (Appels)
            DataTable dtExiste = FonctionsAppels.RechercheHisto("Provenance", tBProvenance.Text);
            if (dtExiste.Rows.Count > 0)
                MessageBox.Show("Impossible de supprimer cette provenance car elle a déjà été utilisée. ", "Suppression", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            else
            {
                //Suppression de l'enregistrement
                if (FonctionsAppels.SupprProvenance(lIdProvenance.Text) == "OK")
                {
                    MessageBox.Show("Suppression de la provenance => OK ", "Suppression", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    VideChamps();
                    //Gestion des boutons
                    bAjouter.Visible = true;
                    bValider.Enabled = false;
                    bCancel.Visible = false;
                    bSupprimer.Enabled = false;

                    Etat = "Lecture";

                    //On rafraichi la liste des provenances
                    FProvenance_Load(sender, e);
                }
            }
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            //on annule
            VideChamps();
            //Gestion des boutons
            bAjouter.Visible = true;
            bValider.Enabled = false;
            bCancel.Visible = false;
            bSupprimer.Enabled = false;

            Etat = "Lecture";

            //On rafraichi la liste des Communes
            FProvenance_Load(sender, e);
        }

        private void bExit_Click(object sender, EventArgs e)
        {
            if (Etat != "Lecture")
            {
                var result = MessageBox.Show("Il y a des modification en cours...Voulez vous les abandonner? ", "Abandon", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                    this.Close();
            }
            else
                this.Close();
        }

        private void tBProvenance_Click(object sender, EventArgs e)
        {
            //si on est en lecture, on passe en ajout
            if (Etat == "Lecture")
            {
                bAjouter_Click(sender, e);
            }
        }

        private void tBCodeProvenance_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                bValider_Click(sender, e);
            }
        }

    }
}