using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using LiteDB;
using System.Web;
using System.IO;


namespace izmirotobus
{
    class Program
    {
        static void Main(string[] args)
        {

            //Hatlar okunuyor.DBe yükleniyor.
            string url = "http://www.eshot.gov.tr/tr/UlasimSaatleri/288";
            string htmlContent = GetContent(url);


            List<Line> LinesList = new List<Line>();

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent);
            var nodes = doc.DocumentNode.SelectNodes("//option");

            foreach (var node in nodes)
            {
                string name = node.NextSibling.InnerText;
                string no = node.Attributes["value"].Value;
                Line nLine = new Line { lineNo = no, lineName = name };

                LinesList.Add(nLine);
                add2linedb(nLine);
            }

            Console.WriteLine("Hat db hazır.");
            Console.ReadLine();

            List<Line> hatlarx = new List<Line>();

            //DBden Hatlar çekiliyor.
            using (var hatdb = new LiteDatabase(@"botart_ulasim_hat.adbx"))
            {
                // Get hat collection

                var hatDb = hatdb.GetCollection<Line>("nLine");


                List<Line> resultshat = new List<Line>();


                resultshat = hatDb.FindAll().ToList();

                if (resultshat != null)
                { hatlarx = resultshat; }
            }

            int sayac1 = 0;

            if (hatlarx == null) { Console.WriteLine("Hatlardb boş."); }
            foreach (var hatt in hatlarx)
            {


                Console.WriteLine("Hat NO:" + hatt.lineNo);
                Console.WriteLine("Hat DETAY:" + hatt.lineName);
                Console.WriteLine("Sayac:" + sayac1);
                sayac1++;

            }

            Console.WriteLine(sayac1);
            Console.ReadLine();


            //Son güncellenme tarihi
            var guncellenmenode = doc.DocumentNode.SelectNodes(@"//span[@class=""text-danger""]");

            foreach (var gnode in guncellenmenode)
            {
                string sonGuncellenmeTarihi = gnode.InnerText;
                char[] delimiter = new char[] { ':' };
                string[] tarih = sonGuncellenmeTarihi.Split(delimiter,
                                             StringSplitOptions.RemoveEmptyEntries);

                Console.WriteLine(tarih[1] + ":" + tarih[2]);
                Console.ReadLine();
            }



            //Otobüs Saatleri Okunuyor. DB oluşturuluyor.

            List<Bus> BusesList = new List<Bus>();

            for (int j = 0; j < LinesList.Count; j++)  //LinesList.Count !!!!
            {
               // j = 318; //deneme
                Bus nextBus = new Bus();
                nextBus.gunler = new List<Gunler>();
                nextBus.busNo = LinesList[j].lineNo;
                nextBus.Busname = LinesList[j].lineName;


                string detailurl = "http://www.eshot.gov.tr/tr/UlasimSaatleri/" + LinesList[j].lineNo + "/288";   //LinesList[j].lineNo  !!!!
                string detailHtmlContent = GetContent(detailurl);

                string value = LinesList[j].lineName;

                char[] delimiters = new char[] { ':', '-' };
                string[] parts = value.Split(delimiters,
                                 StringSplitOptions.RemoveEmptyEntries);

                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document.LoadHtml(detailHtmlContent);

                //HAFTAİÇİ
                var hiciNodes = document.DocumentNode.SelectNodes(@"//div[@id=""eleman1""]").First();

                var hicikalkisNodes = hiciNodes.SelectNodes(".//h4");

              

                Gunler HaftaIci1 = new Gunler();
                HaftaIci1.Saatler = new List<string>();
                Gunler HaftaIci2 = new Gunler();
                HaftaIci2.Saatler = new List<string>();

                if (hiciNodes.SelectNodes(".//ul//span") != null)
                {
                    
                    HtmlNode[] hicisaatNodes = hiciNodes.SelectNodes(".//ul//span").ToArray();

                    int cnt1 = hicisaatNodes.Count();
                    //  Console.WriteLine(cnt1);

                    var parentilk = hicisaatNodes[1].ParentNode.ParentNode;

                    string dnmehtmlilk = parentilk.InnerHtml;
                    // Console.WriteLine(dnmehtmlilk);
                    var htmlDocilk = new HtmlDocument();
                    htmlDocilk.LoadHtml(dnmehtmlilk);
                    var hiciilksaatNodesilk = htmlDocilk.DocumentNode.SelectNodes(@"//span[@class=""pull-left""]").ToArray();

                    foreach (var cnode in hiciilksaatNodesilk)
                    {

                        string dstp = parts[1] + " " + "Kalkış";
                        string h = cnode.InnerText;

                        HaftaIci1.Gun = "Haftaiçi";
                        HaftaIci1.KalkisDuragi = dstp;
                        HaftaIci1.Saatler.Add(h);// = hici1saatler;

                    }

                    var parentiki = hicisaatNodes[cnt1 - 1].ParentNode.ParentNode;

                    string dnmehtmliki = parentiki.InnerHtml;
                    //  Console.WriteLine(dnmehtmliki);
                    var htmlDociki = new HtmlDocument();
                    htmlDociki.LoadHtml(dnmehtmliki);
                    var hiciilksaatNodesiki = htmlDociki.DocumentNode.SelectNodes(@"//span[@class=""pull-left""]").ToArray();


                    foreach (var cnodeiki in hiciilksaatNodesiki)
                    {

                        string dstp = parts[2] + " " + "Kalkış";
                        string h = cnodeiki.InnerText;
                     

                        HaftaIci2.Gun = "Haftaiçi";
                        HaftaIci2.KalkisDuragi = dstp;
                        HaftaIci2.Saatler.Add(h); // = hici2saatler;

                    }

                }

                nextBus.gunler.Add(HaftaIci1);
                nextBus.gunler.Add(HaftaIci2);

                //HAFTAİÇİ SONU

                //CUMARTESİ Günü
                var cmrtNodes = document.DocumentNode.SelectNodes(@"//div[@id=""eleman2""]").First();

                var cmrtkalkisNodes = cmrtNodes.SelectNodes(".//h4");
                Gunler Cumartesi1 = new Gunler();
                Cumartesi1.Saatler = new List<string>();
                Gunler Cumartesi2 = new Gunler();
                Cumartesi2.Saatler = new List<string>();
                if (cmrtNodes.SelectNodes(".//ul//span") != null)
                {
                    HtmlNode[] cmrtsaatNodes = cmrtNodes.SelectNodes(".//ul//span").ToArray();


                    

                    int cnt2 = cmrtsaatNodes.Count();
                    // Console.WriteLine(cnt2);

                    var parentcmrtilk = cmrtsaatNodes[1].ParentNode.ParentNode;

                    string dnmehtmlcmrtilk = parentcmrtilk.InnerHtml;
                    // Console.WriteLine(dnmehtmlcmrtilk);
                    var htmlDoccmrtilk = new HtmlDocument();
                    htmlDoccmrtilk.LoadHtml(dnmehtmlcmrtilk);
                    var cmrtilksaatNodesilk = htmlDoccmrtilk.DocumentNode.SelectNodes(@"//span[@class=""pull-left""]").ToArray();

                    foreach (var cnode in cmrtilksaatNodesilk)
                    {

                        string dstp = parts[1] + " " + "Kalkış";
                        string h = cnode.InnerText;
                    

                        Cumartesi1.Gun = "Cumartesi";
                        Cumartesi1.KalkisDuragi = dstp;
                        Cumartesi1.Saatler.Add(h); // = cmrt1saatler;

                    }

                    var parentcmrtiki = cmrtsaatNodes[cnt2 - 1].ParentNode.ParentNode;

                    string dnmehtmlcmrtiki = parentcmrtiki.InnerHtml;
                    // Console.WriteLine(dnmehtmlcmrtiki);
                    var htmlDoccmrtiki = new HtmlDocument();
                    htmlDoccmrtiki.LoadHtml(dnmehtmlcmrtiki);
                    var cmrtilksaatNodesiki = htmlDoccmrtiki.DocumentNode.SelectNodes(@"//span[@class=""pull-left""]").ToArray();
                    foreach (var cnodeiki in cmrtilksaatNodesiki)
                    {

                        string dstp = parts[2] + " " + "Kalkış";
                        string h = cnodeiki.InnerText;
               

                        Cumartesi2.Gun = "Cumartesi";
                        Cumartesi2.KalkisDuragi = dstp;
                        Cumartesi2.Saatler.Add(h); // = cmrt2saatler;

                    }
                }

                nextBus.gunler.Add(Cumartesi1);
                nextBus.gunler.Add(Cumartesi2);
                //CUMARTESİ Günü SONU


                //PAZAR Günü
                var pzrNodes = document.DocumentNode.SelectNodes(@"//div[@id=""eleman3""]").First();

                var pzrkalkisNodes = pzrNodes.SelectNodes(".//h4");
                Gunler Pazar1 = new Gunler();
                Pazar1.Saatler = new List<string>();
                Gunler Pazar2 = new Gunler();
                Pazar2.Saatler = new List<string>();
                if (pzrNodes.SelectNodes(".//ul//span") != null)
                {

                    HtmlNode[] pzrsaatNodes = pzrNodes.SelectNodes(".//ul//span").ToArray();


          

                    int cnt3 = pzrsaatNodes.Count();

                    // Console.WriteLine(cnt3);

                    var parentpzrilk = pzrsaatNodes[1].ParentNode.ParentNode;

                    string dnmehtmlpzrilk = parentpzrilk.InnerHtml;
                    //  Console.WriteLine(dnmehtmlpzrilk);
                    var htmlDocpzrilk = new HtmlDocument();
                    htmlDocpzrilk.LoadHtml(dnmehtmlpzrilk);
                    var pzrilksaatNodesilk = htmlDocpzrilk.DocumentNode.SelectNodes(@"//span[@class=""pull-left""]").ToArray();

                    foreach (var cnode in pzrilksaatNodesilk)
                    {

                        string dstp = parts[1] + " " + "Kalkış";
                        string h = cnode.InnerText;
                    
                        Pazar1.Gun = "Pazar";
                        Pazar1.KalkisDuragi = dstp;
                        Pazar1.Saatler.Add(h); // = pzr1saatler;


                    }

                    var parentpzriki = pzrsaatNodes[cnt3 - 1].ParentNode.ParentNode;

                    string dnmehtmlpzriki = parentpzriki.InnerHtml;
                    //  Console.WriteLine(dnmehtmlpzriki);
                    var htmlDocpzriki = new HtmlDocument();
                    htmlDocpzriki.LoadHtml(dnmehtmlpzriki);
                    var pzrilksaatNodesiki = htmlDocpzriki.DocumentNode.SelectNodes(@"//span[@class=""pull-left""]").ToArray();
                    foreach (var cnodeiki in pzrilksaatNodesiki)
                    {

                        string dstp = parts[2] + " " + "Kalkış";
                        string h = cnodeiki.InnerText;
                 
                        Pazar2.Gun = "Pazar";
                        Pazar2.KalkisDuragi = dstp;
                        Pazar2.Saatler.Add(h); // = pzr2saatler;

                    }
                }


                nextBus.gunler.Add(Pazar1);
                nextBus.gunler.Add(Pazar2);
                //PAZAR Günü SONU 



                ////Otobüsler Dbye ekleniyor.

                add2busdb(nextBus);
               
   }
            
            Console.WriteLine("Otobus db hazir.");
            Console.ReadLine();

        }

        private static void add2linedb(Line h)
        {
            if (h == null)
            {
                Console.WriteLine("NULL");
                return;

            }

            // Open database (or create if not exits)
            using (var db = new LiteDatabase(@"botart_ulasim_hat.adbx"))
            {
                // Get hat collection
                var hatlar = db.GetCollection<Line>("nLine");

                var results = hatlar.Find(x => x.lineNo == h.lineNo).FirstOrDefault();
                if (results == null)
                {
                    h._id = Guid.NewGuid().ToString();
                    hatlar.Insert(h);
                }
                else
                {
                    //Burada hattın bilgileri guncellenebilir
                    Console.WriteLine("UPDATING " + results._id);
                    hatlar.Update(results._id, h);
                }

                //hatlar.EnsureIndex(x => x.lineNo);
            }

        }

        private static void add2busdb(Bus o)
        {
            if (o == null)
            {
                Console.WriteLine("NULL");
                return;

            }
            // Open database (or create if not exits)



            using (var db = new LiteDatabase(@"botart_ulasim_bus.adbx"))
            {
                // Get hat collection
                var dbo = db.GetCollection<Bus>("otobus");
                var results = dbo.Find(x => x.busNo == o.busNo).FirstOrDefault();
                if (results == null)
                {

                    Console.WriteLine(o.busNo);
                Console.WriteLine(o.gunler.Count);
                    o._id = Guid.NewGuid().ToString();
                    dbo.Insert(o);
                }
                else
                {
                    //Burada hattın bilgileri guncellenebilir
                    Console.WriteLine("UPDATING " + results._id);
                    dbo.Update(results._id, o);
                }
                dbo.EnsureIndex(x => x.busNo);
                dbo.EnsureIndex(x => x.Busname);
            }
        }

        private static string GetContent(string urlAddress)
        {
            Uri url = new Uri(urlAddress);
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.UTF8;
            string html = client.DownloadString(url);

            return html;
        }

    }

    public class Line
    {
        public string _id { get; set; }
        public string lineNo { get; set; }
        public string lineName { get; set; }
    }


    public class Gunler
    {
        public string KalkisDuragi { get; set; }
        public string Gun { get; set; }
        public List<string> Saatler { get; set; }
    }

    public class Bus
    {
        public string _id { get; set; }
        public string busNo { get; set; }
        public string Busname { get; set; }
        public List<Gunler> gunler { get; set; }


    }








}

