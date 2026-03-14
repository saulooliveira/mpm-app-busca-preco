namespace BuscaPreco.Presentation.WindowsForms
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lista = new System.Windows.Forms.ListBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lpreco = new System.Windows.Forms.TextBox();
            this.ldescricao = new System.Windows.Forms.TextBox();
            this.lbarras = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tempo = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.bTexto = new System.Windows.Forms.Button();
            this.linha2 = new System.Windows.Forms.TextBox();
            this.linha1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.bReset = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.bParam = new System.Windows.Forms.Button();
            this.busca = new System.Windows.Forms.CheckBox();
            this.dinamico = new System.Windows.Forms.CheckBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.bUpdate = new System.Windows.Forms.Button();
            this.senha = new System.Windows.Forms.TextBox();
            this.usuario = new System.Windows.Forms.TextBox();
            this.ftpserv = new System.Windows.Forms.TextBox();
            this.nome = new System.Windows.Forms.TextBox();
            this.servnomes = new System.Windows.Forms.TextBox();
            this.gateway = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.bConfig = new System.Windows.Forms.Button();
            this.time = new System.Windows.Forms.TextBox();
            this.l4 = new System.Windows.Forms.TextBox();
            this.l3 = new System.Windows.Forms.TextBox();
            this.l2 = new System.Windows.Forms.TextBox();
            this.l1 = new System.Windows.Forms.TextBox();
            this.mascara = new System.Windows.Forms.TextBox();
            this.ipservidor = new System.Windows.Forms.TextBox();
            this.ipcliente = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // lista
            // 
            this.lista.FormattingEnabled = true;
            this.lista.Location = new System.Drawing.Point(12, 12);
            this.lista.Name = "lista";
            this.lista.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lista.Size = new System.Drawing.Size(178, 329);
            this.lista.TabIndex = 0;
            this.lista.SelectedIndexChanged += new System.EventHandler(this.lista_SelectedIndexChanged);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(196, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(430, 327);
            this.tabControl1.TabIndex = 3;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.textBox1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(422, 301);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Histórico";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(6, 6);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(410, 289);
            this.textBox1.TabIndex = 0;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupBox2);
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(422, 301);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Mensagens";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lpreco);
            this.groupBox2.Controls.Add(this.ldescricao);
            this.groupBox2.Controls.Add(this.lbarras);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Location = new System.Drawing.Point(6, 114);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(181, 97);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Consulta";
            // 
            // lpreco
            // 
            this.lpreco.Location = new System.Drawing.Point(50, 67);
            this.lpreco.Name = "lpreco";
            this.lpreco.Size = new System.Drawing.Size(122, 20);
            this.lpreco.TabIndex = 5;
            // 
            // ldescricao
            // 
            this.ldescricao.Location = new System.Drawing.Point(69, 39);
            this.ldescricao.Name = "ldescricao";
            this.ldescricao.Size = new System.Drawing.Size(103, 20);
            this.ldescricao.TabIndex = 4;
            // 
            // lbarras
            // 
            this.lbarras.Location = new System.Drawing.Point(53, 13);
            this.lbarras.Name = "lbarras";
            this.lbarras.Size = new System.Drawing.Size(119, 20);
            this.lbarras.TabIndex = 3;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(7, 74);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(38, 13);
            this.label6.TabIndex = 2;
            this.label6.Text = "Preço:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 46);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(58, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "Descrição:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 16);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(40, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Barras:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tempo);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.bTexto);
            this.groupBox1.Controls.Add(this.linha2);
            this.groupBox1.Controls.Add(this.linha1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(6, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(247, 102);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Texto instantâneo";
            // 
            // tempo
            // 
            this.tempo.Location = new System.Drawing.Point(58, 72);
            this.tempo.Name = "tempo";
            this.tempo.Size = new System.Drawing.Size(69, 20);
            this.tempo.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(43, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Tempo:";
            // 
            // bTexto
            // 
            this.bTexto.Location = new System.Drawing.Point(133, 70);
            this.bTexto.Name = "bTexto";
            this.bTexto.Size = new System.Drawing.Size(108, 23);
            this.bTexto.TabIndex = 3;
            this.bTexto.Text = "Enviar";
            this.bTexto.UseVisualStyleBackColor = true;
            this.bTexto.Click += new System.EventHandler(this.bTexto_Click);
            // 
            // linha2
            // 
            this.linha2.Location = new System.Drawing.Point(58, 46);
            this.linha2.Name = "linha2";
            this.linha2.Size = new System.Drawing.Size(183, 20);
            this.linha2.TabIndex = 2;
            // 
            // linha1
            // 
            this.linha1.Location = new System.Drawing.Point(58, 20);
            this.linha1.Name = "linha1";
            this.linha1.Size = new System.Drawing.Size(183, 20);
            this.linha1.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Linha 2:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Linha 1:";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.bReset);
            this.tabPage3.Controls.Add(this.groupBox5);
            this.tabPage3.Controls.Add(this.groupBox4);
            this.tabPage3.Controls.Add(this.groupBox3);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(422, 301);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Configurações";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // bReset
            // 
            this.bReset.Location = new System.Drawing.Point(3, 270);
            this.bReset.Name = "bReset";
            this.bReset.Size = new System.Drawing.Size(222, 23);
            this.bReset.TabIndex = 3;
            this.bReset.Text = "Reset";
            this.bReset.UseVisualStyleBackColor = true;
            this.bReset.Click += new System.EventHandler(this.bReset_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.bParam);
            this.groupBox5.Controls.Add(this.busca);
            this.groupBox5.Controls.Add(this.dinamico);
            this.groupBox5.Location = new System.Drawing.Point(231, 222);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(188, 67);
            this.groupBox5.TabIndex = 2;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "ParamConfig";
            // 
            // bParam
            // 
            this.bParam.Location = new System.Drawing.Point(9, 38);
            this.bParam.Name = "bParam";
            this.bParam.Size = new System.Drawing.Size(173, 23);
            this.bParam.TabIndex = 18;
            this.bParam.Text = "Enviar";
            this.bParam.UseVisualStyleBackColor = true;
            this.bParam.Click += new System.EventHandler(this.bParam_Click);
            // 
            // busca
            // 
            this.busca.AutoSize = true;
            this.busca.Location = new System.Drawing.Point(90, 19);
            this.busca.Name = "busca";
            this.busca.Size = new System.Drawing.Size(98, 17);
            this.busca.TabIndex = 17;
            this.busca.Text = "Busca Servidor";
            this.busca.UseVisualStyleBackColor = true;
            // 
            // dinamico
            // 
            this.dinamico.AutoSize = true;
            this.dinamico.Location = new System.Drawing.Point(9, 19);
            this.dinamico.Name = "dinamico";
            this.dinamico.Size = new System.Drawing.Size(83, 17);
            this.dinamico.TabIndex = 16;
            this.dinamico.Text = "IP Dinâmico";
            this.dinamico.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.bUpdate);
            this.groupBox4.Controls.Add(this.senha);
            this.groupBox4.Controls.Add(this.usuario);
            this.groupBox4.Controls.Add(this.ftpserv);
            this.groupBox4.Controls.Add(this.nome);
            this.groupBox4.Controls.Add(this.servnomes);
            this.groupBox4.Controls.Add(this.gateway);
            this.groupBox4.Controls.Add(this.label20);
            this.groupBox4.Controls.Add(this.label19);
            this.groupBox4.Controls.Add(this.label18);
            this.groupBox4.Controls.Add(this.label17);
            this.groupBox4.Controls.Add(this.label16);
            this.groupBox4.Controls.Add(this.label15);
            this.groupBox4.Location = new System.Drawing.Point(231, 3);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(188, 213);
            this.groupBox4.TabIndex = 1;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "UpdConfig";
            // 
            // bUpdate
            // 
            this.bUpdate.Location = new System.Drawing.Point(9, 179);
            this.bUpdate.Name = "bUpdate";
            this.bUpdate.Size = new System.Drawing.Size(173, 23);
            this.bUpdate.TabIndex = 15;
            this.bUpdate.Text = "Enviar";
            this.bUpdate.UseVisualStyleBackColor = true;
            this.bUpdate.Click += new System.EventHandler(this.bUpdate_Click);
            // 
            // senha
            // 
            this.senha.Location = new System.Drawing.Point(53, 152);
            this.senha.Name = "senha";
            this.senha.Size = new System.Drawing.Size(129, 20);
            this.senha.TabIndex = 14;
            // 
            // usuario
            // 
            this.usuario.Location = new System.Drawing.Point(58, 126);
            this.usuario.Name = "usuario";
            this.usuario.Size = new System.Drawing.Size(124, 20);
            this.usuario.TabIndex = 13;
            // 
            // ftpserv
            // 
            this.ftpserv.Location = new System.Drawing.Point(70, 100);
            this.ftpserv.Name = "ftpserv";
            this.ftpserv.Size = new System.Drawing.Size(112, 20);
            this.ftpserv.TabIndex = 12;
            // 
            // nome
            // 
            this.nome.Location = new System.Drawing.Point(50, 74);
            this.nome.Name = "nome";
            this.nome.Size = new System.Drawing.Size(132, 20);
            this.nome.TabIndex = 11;
            // 
            // servnomes
            // 
            this.servnomes.Location = new System.Drawing.Point(83, 48);
            this.servnomes.Name = "servnomes";
            this.servnomes.Size = new System.Drawing.Size(99, 20);
            this.servnomes.TabIndex = 10;
            // 
            // gateway
            // 
            this.gateway.Location = new System.Drawing.Point(64, 22);
            this.gateway.Name = "gateway";
            this.gateway.Size = new System.Drawing.Size(118, 20);
            this.gateway.TabIndex = 9;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(6, 81);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(38, 13);
            this.label20.TabIndex = 5;
            this.label20.Text = "Nome:";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(6, 159);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(41, 13);
            this.label19.TabIndex = 4;
            this.label19.Text = "Senha:";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(6, 133);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(46, 13);
            this.label18.TabIndex = 3;
            this.label18.Text = "Usuário:";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(6, 107);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(58, 13);
            this.label17.TabIndex = 2;
            this.label17.Text = "FTP Serv.:";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(6, 55);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(71, 13);
            this.label16.TabIndex = 1;
            this.label16.Text = "Serv. Nomes:";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(6, 29);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(52, 13);
            this.label15.TabIndex = 0;
            this.label15.Text = "Gateway:";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.bConfig);
            this.groupBox3.Controls.Add(this.time);
            this.groupBox3.Controls.Add(this.l4);
            this.groupBox3.Controls.Add(this.l3);
            this.groupBox3.Controls.Add(this.l2);
            this.groupBox3.Controls.Add(this.l1);
            this.groupBox3.Controls.Add(this.mascara);
            this.groupBox3.Controls.Add(this.ipservidor);
            this.groupBox3.Controls.Add(this.ipcliente);
            this.groupBox3.Controls.Add(this.label14);
            this.groupBox3.Controls.Add(this.label13);
            this.groupBox3.Controls.Add(this.label12);
            this.groupBox3.Controls.Add(this.label11);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.label8);
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Location = new System.Drawing.Point(3, 3);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(222, 261);
            this.groupBox3.TabIndex = 0;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Config02";
            // 
            // bConfig
            // 
            this.bConfig.Location = new System.Drawing.Point(9, 229);
            this.bConfig.Name = "bConfig";
            this.bConfig.Size = new System.Drawing.Size(201, 23);
            this.bConfig.TabIndex = 8;
            this.bConfig.Text = "Enviar";
            this.bConfig.UseVisualStyleBackColor = true;
            this.bConfig.Click += new System.EventHandler(this.button1_Click);
            // 
            // time
            // 
            this.time.Location = new System.Drawing.Point(57, 203);
            this.time.Name = "time";
            this.time.Size = new System.Drawing.Size(54, 20);
            this.time.TabIndex = 7;
            // 
            // l4
            // 
            this.l4.Location = new System.Drawing.Point(57, 177);
            this.l4.Name = "l4";
            this.l4.Size = new System.Drawing.Size(153, 20);
            this.l4.TabIndex = 6;
            // 
            // l3
            // 
            this.l3.Location = new System.Drawing.Point(57, 151);
            this.l3.Name = "l3";
            this.l3.Size = new System.Drawing.Size(153, 20);
            this.l3.TabIndex = 5;
            // 
            // l2
            // 
            this.l2.Location = new System.Drawing.Point(57, 125);
            this.l2.Name = "l2";
            this.l2.Size = new System.Drawing.Size(153, 20);
            this.l2.TabIndex = 4;
            // 
            // l1
            // 
            this.l1.Location = new System.Drawing.Point(57, 99);
            this.l1.Name = "l1";
            this.l1.Size = new System.Drawing.Size(153, 20);
            this.l1.TabIndex = 3;
            // 
            // mascara
            // 
            this.mascara.Location = new System.Drawing.Point(104, 73);
            this.mascara.Name = "mascara";
            this.mascara.Size = new System.Drawing.Size(106, 20);
            this.mascara.TabIndex = 2;
            // 
            // ipservidor
            // 
            this.ipservidor.Location = new System.Drawing.Point(74, 22);
            this.ipservidor.Name = "ipservidor";
            this.ipservidor.Size = new System.Drawing.Size(136, 20);
            this.ipservidor.TabIndex = 1;
            // 
            // ipcliente
            // 
            this.ipcliente.Location = new System.Drawing.Point(74, 47);
            this.ipcliente.Name = "ipcliente";
            this.ipcliente.Size = new System.Drawing.Size(136, 20);
            this.ipcliente.TabIndex = 0;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(6, 210);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(43, 13);
            this.label14.TabIndex = 7;
            this.label14.Text = "Tempo:";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(6, 184);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(45, 13);
            this.label13.TabIndex = 6;
            this.label13.Text = "Linha 4:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 158);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(45, 13);
            this.label12.TabIndex = 5;
            this.label12.Text = "Linha 3:";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 132);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(45, 13);
            this.label11.TabIndex = 4;
            this.label11.Text = "Linha 2:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 106);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(45, 13);
            this.label10.TabIndex = 3;
            this.label10.Text = "Linha 1:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 80);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(95, 13);
            this.label9.TabIndex = 2;
            this.label9.Text = "Máscara de Rede:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 54);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(63, 13);
            this.label8.TabIndex = 1;
            this.label8.Text = "IP Terminal:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 29);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(62, 13);
            this.label7.TabIndex = 0;
            this.label7.Text = "IP Servidor:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(638, 346);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.lista);
            this.Name = "Form1";
            this.Text = "Servidor Busca-Preço";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox lista;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox tempo;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button bTexto;
        private System.Windows.Forms.TextBox linha2;
        private System.Windows.Forms.TextBox linha1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox mascara;
        private System.Windows.Forms.TextBox ipservidor;
        private System.Windows.Forms.TextBox ipcliente;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox time;
        private System.Windows.Forms.TextBox l4;
        private System.Windows.Forms.TextBox l3;
        private System.Windows.Forms.TextBox l2;
        private System.Windows.Forms.TextBox l1;
        private System.Windows.Forms.Button bConfig;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.TextBox ftpserv;
        private System.Windows.Forms.TextBox nome;
        private System.Windows.Forms.TextBox servnomes;
        private System.Windows.Forms.TextBox gateway;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Button bReset;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button bParam;
        private System.Windows.Forms.CheckBox busca;
        private System.Windows.Forms.CheckBox dinamico;
        private System.Windows.Forms.Button bUpdate;
        private System.Windows.Forms.TextBox senha;
        private System.Windows.Forms.TextBox usuario;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox lpreco;
        private System.Windows.Forms.TextBox ldescricao;
        private System.Windows.Forms.TextBox lbarras;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
    }
}

