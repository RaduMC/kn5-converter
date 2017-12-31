using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace kn5_converter
{
    class Program
    {
        public class kn5Model
        {
            public string modelDir;
            public string modelName;

            public int version;
            public List<string> textures = new List<string>();
            public List<kn5Texture> usedTex = new List<kn5Texture>();
            public List<kn5Material> materials = new List<kn5Material>();
            public List<kn5Node> nodes = new List<kn5Node>();
        }

        public class kn5Material
        {
            public string name = "Default";
            public string shader = "";
            public float ksAmbient = 0.6f;
            public float ksDiffuse = 0.6f;
            public float ksSpecular = 0.9f;
            public float ksSpecularEXP = 1.0f;
            public float diffuseMult = 1.0f;
            public float normalMult = 1.0f;
            public float useDetail = 0.0f;
            public float detailUVMultiplier = 1.0f;

            public string txDiffuse;
            public string txNormal;
            public string txDetail;

            public string shaderProps = "";

            public int alphaBlend = 0;
            public int alphaTest = 0;
            public int depthMode = 0;
            public List<Var> var = new List<Var>();
            public List<Res> res = new List<Res>(); 
        }

        public class Var
        {
            public string name;
            public float value;
        }
        public class Res
        {
            public string name;
            public int slot;
            public string texture;
        }

        public class kn5Texture
        {
            public string filename;
            public float UVScaling = 1.0f;
        }

        public class kn5Node
        {
            public int type = 1;
            public string name = "Default";

            public float[,] tmatrix = new float[4, 4] { { 1.0f, 0.0f, 0.0f, 0.0f }, { 0.0f, 1.0f, 0.0f, 0.0f }, { 0.0f, 0.0f, 1.0f, 0.0f }, { 0.0f, 0.0f, 0.0f, 1.0f } };
            public float[,] hmatrix = new float[4, 4] { { 1.0f, 0.0f, 0.0f, 0.0f }, { 0.0f, 1.0f, 0.0f, 0.0f }, { 0.0f, 0.0f, 1.0f, 0.0f }, { 0.0f, 0.0f, 0.0f, 1.0f } };

            public float[] translation = new float[3] { 0.0f, 0.0f, 0.0f };
            public float[] rotation = new float[3] { 0.0f, 0.0f, 0.0f };
            public float[] scaling = new float[3] { 1.0f, 1.0f, 1.0f };

            public int vertexCount;
            public float[] position;
            public float[] normal;
            public float[] texture0;

            public ushort[] indices;

            public int materialID = -1;

            //public List<kn5Node> children; //do I really wanna do this? no
            public int parentID = -1;
        }

        static string[] outputTypes = new string[3] {"fbx", "obj", "objZMhack"};
        //static string currentPath = AppDomain.CurrentDomain.BaseDirectory;

        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            SearchOption recurse = SearchOption.AllDirectories;

            //string input = @"D:\Assetto Corsa\content\";
            string input = "";

            switch (args.Length)
            {
                case 0:
                    input = AppDomain.CurrentDomain.BaseDirectory;
                    recurse = SearchOption.TopDirectoryOnly;
                    goto default;
                case 1:
                    if (GetOutputTypes(args[0])) { goto case 0; }
                    else { input = args[0]; }
                    break;
                case 2:
                    if (GetOutputTypes(args[0])) { input = args[1]; }
                    else if (GetOutputTypes(args[1])) { input = args[0]; } //maybe the user can't follow instructions
                    break;
                default:
                    Console.WriteLine("Assetto Corsa kn5 model converter\nby Chipicao - KotsChopShop.com\n\nUsage: kn5conv.exe [-fbx|-obj|-objZMhack] [input_file/folder]");
                    break;
            }

            if (File.Exists(input))
            {
                input = Path.GetFullPath(input); //in case only filename is entered

                string currentModel = MakeRelative(input, (Path.GetDirectoryName(input) + "\\"));
                Console.WriteLine("Reading {0}", currentModel);

                var theModel = readKN5(input);
                //Console.WriteLine("\rConverting {0}", currentModel);

                foreach (string format in outputTypes)
                {
                    switch (format.ToLower())
                    {
                        case "fbx":
                            ExportFBX(theModel);
                            ExportIni(theModel);
                            break;
                        case "obj":
                            ExportOBJ(theModel, false);
                            break;
                        case "objzmhack":
                            ExportOBJ(theModel, true);
                            break;
                    }
                }
            }
            else if (Directory.Exists(input))
            {
                input = Path.GetFullPath(input + "\\");
                string[] inputFiles = Directory.GetFiles(input, "*.kn5", recurse);
                Console.WriteLine("Found {0} files.", inputFiles.Length);

                foreach (var inputFile in inputFiles)
                {
                    string currentModel = MakeRelative(inputFile, input);
                    Console.WriteLine("Reading {0}", currentModel);

                    var theModel = readKN5(inputFile);
                    //Console.WriteLine("\rConverting {0}", currentModel);

                    foreach (string format in outputTypes)
                    {
                        switch (format.ToLower())
                        {
                            case "fbx":
                                ExportFBX(theModel);
                                ExportIni(theModel);
                                break;
                            case "obj":
                                ExportOBJ(theModel, false);
                                break;
                            case "objzmhack":
                                ExportOBJ(theModel, true);
                                break;
                        }
                    }
                }

                Console.WriteLine("Finished. Press any key to exit...");
                Console.ReadKey();
            }
            else { Console.WriteLine("Invalid input file/folder: {0}\n\nUsage: M3G2FBX.exe [-u] [input_file/folder]", input); }
        }

        private static bool GetOutputTypes(string arg)
        {
            string[] outputArgs = arg.Split(new char[1] { '-' });

            //don't just split the types, also check if they are valid
            //TODO make case insensitive
            string[] newOutputTypes = outputArgs.Intersect(outputTypes, StringComparer.OrdinalIgnoreCase).ToArray();

            if (newOutputTypes.Length > 0)
            {
                outputTypes = newOutputTypes;
                return true;
            }
            else { return false; }
        }


        private static kn5Model readKN5(string kn5File)
        {
            using (BinaryReader binStream = new BinaryReader(File.OpenRead(kn5File)))
            {
                string magicNumber = ReadStr(binStream, 6);
                if (magicNumber == "sc6969")
                {
                    kn5Model newModel = new kn5Model();
                    newModel.modelDir = Path.GetDirectoryName(kn5File) + "\\";
                    newModel.modelName = Path.GetFileNameWithoutExtension(kn5File);

                    newModel.version = binStream.ReadInt32();
                    if (newModel.version > 5) { int unknownNo = binStream.ReadInt32(); } //673425

                    #region extract textures
                    Directory.CreateDirectory(newModel.modelDir + "texture");
                    int texCount = binStream.ReadInt32();
                    for (int t = 0; t < texCount; t++)
                    {
                        int texType = binStream.ReadInt32();
                        string texName = ReadStr(binStream, binStream.ReadInt32());
                        int texSize = binStream.ReadInt32();
                        newModel.textures.Add(texName);

                        if (File.Exists(newModel.modelDir + "texture\\" + texName))
                        {
                            binStream.BaseStream.Position += texSize;
                        }
                        else
                        {
                            byte[] texBuffer = binStream.ReadBytes(texSize);
                            using (BinaryWriter texWriter = new BinaryWriter(File.Create(newModel.modelDir + "texture\\" + texName)))
                            {
                                texWriter.Write(texBuffer);
                            }
                        }
                    }
                    #endregion

                    #region read materials
                    int matCount = binStream.ReadInt32();
                    for (int m = 0; m < matCount; m++)
                    {
                        kn5Material newMaterial = new kn5Material();

                        newMaterial.name = ReadStr(binStream, binStream.ReadInt32());
                        newMaterial.shader = ReadStr(binStream, binStream.ReadInt32());

                        byte alphaBlend = binStream.ReadByte();
                        newMaterial.alphaBlend = alphaBlend;
                        byte alphaTest = binStream.ReadByte();
                        newMaterial.alphaTest = alphaTest;

                        if (newModel.version > 4)
                        {
                            byte depthMode = binStream.ReadByte();
                            newMaterial.depthMode = depthMode;
                            binStream.BaseStream.Position += 3;
                        }

                        int propCount = binStream.ReadInt32();
                        for (int p = 0; p < propCount; p++)
                        {
                            string propName = ReadStr(binStream, binStream.ReadInt32());
                            float propValue = binStream.ReadSingle();
                            newMaterial.shaderProps += propName + " = " + propValue.ToString() + "&cr;&lf;";

                            Var newVar = new Var();
                            newVar.name = propName;
                            newVar.value = propValue;
                            newMaterial.var.Add(newVar);

                            switch (propName)
                            {
                                case "ksAmbient":
                                    newMaterial.ksAmbient = propValue;
                                    break;
                                case "ksDiffuse":
                                    newMaterial.ksDiffuse = propValue;
                                    break;
                                case "ksSpecular":
                                    newMaterial.ksSpecular = propValue;
                                    break;
                                case "ksSpecularEXP":
                                    newMaterial.ksSpecularEXP = propValue;
                                    break;
                                case "diffuseMult":
                                    newMaterial.diffuseMult = propValue;
                                    break;
                                case "normalMult":
                                    newMaterial.normalMult = propValue;
                                    break;
                                case "useDetail":
                                    newMaterial.useDetail = propValue;
                                    break;
                                case "detailUVMultiplier":
                                    newMaterial.detailUVMultiplier = propValue;
                                    break;
                            }

                            binStream.BaseStream.Position += 36;
                        }

                        int textures = binStream.ReadInt32();
                        for (int t = 0; t < textures; t++)
                        {
                            string sampleName = ReadStr(binStream, binStream.ReadInt32());
                            int sampleSlot = binStream.ReadInt32();
                            string texName = ReadStr(binStream, binStream.ReadInt32());

                            newMaterial.shaderProps += sampleName + " = " + texName + "&cr;&lf;";

                            Res newRes = new Res();
                            newRes.name = sampleName;
                            newRes.slot = sampleSlot;
                            newRes.texture = texName;
                            newMaterial.res.Add(newRes);

                            switch (sampleName)
                            {
                                case "txDiffuse":
                                    newMaterial.txDiffuse = texName;
                                    break;
                                case "txNormal":
                                    newMaterial.txNormal = texName;
                                    break;
                                case "txDetail":
                                    newMaterial.txDetail = texName;
                                    break;
                            }
                        }

                        newModel.materials.Add(newMaterial);
                    }
                    #endregion

                    readNodes(binStream, newModel.nodes, -1); //recursive

                    return newModel;
                }
                else
                {
                    Console.WriteLine("Unknown file type.");
                    return null;
                }
            }
        }

        private static void readNodes(BinaryReader modelStream, List<kn5Node> nodeList, int parentID)
        {
            kn5Node newNode = new kn5Node();
            newNode.parentID = parentID;

            newNode.type = modelStream.ReadInt32();
            newNode.name = ReadStr(modelStream, modelStream.ReadInt32());
            int childrenCount = modelStream.ReadInt32();
            byte abyte = modelStream.ReadByte();

            switch (newNode.type)
            {
                #region dummy node
                case 1: //dummy
                    {
                        newNode.tmatrix[0, 0] = modelStream.ReadSingle();
                        newNode.tmatrix[0, 1] = modelStream.ReadSingle();
                        newNode.tmatrix[0, 2] = modelStream.ReadSingle();
                        newNode.tmatrix[0, 3] = modelStream.ReadSingle();
                        newNode.tmatrix[1, 0] = modelStream.ReadSingle();
                        newNode.tmatrix[1, 1] = modelStream.ReadSingle();
                        newNode.tmatrix[1, 2] = modelStream.ReadSingle();
                        newNode.tmatrix[1, 3] = modelStream.ReadSingle();
                        newNode.tmatrix[2, 0] = modelStream.ReadSingle();
                        newNode.tmatrix[2, 1] = modelStream.ReadSingle();
                        newNode.tmatrix[2, 2] = modelStream.ReadSingle();
                        newNode.tmatrix[2, 3] = modelStream.ReadSingle();
                        newNode.tmatrix[3, 0] = modelStream.ReadSingle();
                        newNode.tmatrix[3, 1] = modelStream.ReadSingle();
                        newNode.tmatrix[3, 2] = modelStream.ReadSingle();
                        newNode.tmatrix[3, 3] = modelStream.ReadSingle();

                        newNode.translation = new float[3] { newNode.tmatrix[3, 0], newNode.tmatrix[3, 1], newNode.tmatrix[3, 2] };
                        newNode.rotation = MatrixToEuler(newNode.tmatrix);
                        newNode.scaling = ScaleFromMatrix(newNode.tmatrix);
                    
                        break;
                    }
                #endregion
                #region mesh node
                case 2: //mesh
                    {
                        byte bbyte = modelStream.ReadByte();
                        byte cbyte = modelStream.ReadByte();
                        byte dbyte = modelStream.ReadByte();

                        newNode.vertexCount = modelStream.ReadInt32();
                        newNode.position = new float[newNode.vertexCount * 3];
                        newNode.normal = new float[newNode.vertexCount * 3];
                        newNode.texture0 = new float[newNode.vertexCount * 2];

                        for (int v = 0; v < newNode.vertexCount; v++)
                        {
                            newNode.position[v * 3] = modelStream.ReadSingle();
                            newNode.position[v * 3 + 1] = modelStream.ReadSingle();
                            newNode.position[v * 3 + 2] = modelStream.ReadSingle();

                            newNode.normal[v * 3] = modelStream.ReadSingle();
                            newNode.normal[v * 3 + 1] = modelStream.ReadSingle();
                            newNode.normal[v * 3 + 2] = modelStream.ReadSingle();

                            newNode.texture0[v * 2] = modelStream.ReadSingle();
                            newNode.texture0[v * 2 + 1] = 1 - modelStream.ReadSingle();

                            modelStream.BaseStream.Position += 12; //tangents
                        }

                        int indexCount = modelStream.ReadInt32();
                        newNode.indices = new ushort[indexCount];
                        for (int i = 0; i < indexCount; i++)
                        {
                            newNode.indices[i] = modelStream.ReadUInt16();
                        }

                        newNode.materialID = modelStream.ReadInt32();
                        modelStream.BaseStream.Position += 29;

                        break;
                    }
                #endregion
                #region animated mesh
                case 3: //animated mesh
                    {
                        byte bbyte = modelStream.ReadByte();
                        byte cbyte = modelStream.ReadByte();
                        byte dbyte = modelStream.ReadByte();

                        int boneCount = modelStream.ReadInt32();
                        for (int b = 0; b < boneCount; b++)
                        {
                            string boneName = ReadStr(modelStream, modelStream.ReadInt32());
                            modelStream.BaseStream.Position += 64; //transformation matrix
                        }

                        newNode.vertexCount = modelStream.ReadInt32();
                        newNode.position = new float[newNode.vertexCount * 3];
                        newNode.normal = new float[newNode.vertexCount * 3];
                        newNode.texture0 = new float[newNode.vertexCount * 2];

                        for (int v = 0; v < newNode.vertexCount; v++)
                        {
                            newNode.position[v * 3] = modelStream.ReadSingle();
                            newNode.position[v * 3 + 1] = modelStream.ReadSingle();
                            newNode.position[v * 3 + 2] = modelStream.ReadSingle();

                            newNode.normal[v * 3] = modelStream.ReadSingle();
                            newNode.normal[v * 3 + 1] = modelStream.ReadSingle();
                            newNode.normal[v * 3 + 2] = modelStream.ReadSingle();

                            newNode.texture0[v * 2] = modelStream.ReadSingle();
                            newNode.texture0[v * 2 + 1] = 1 - modelStream.ReadSingle();

                            modelStream.BaseStream.Position += 44; //tangents & weights
                        }

                        int indexCount = modelStream.ReadInt32();
                        newNode.indices = new ushort[indexCount];
                        for (int i = 0; i < indexCount; i++)
                        {
                            newNode.indices[i] = modelStream.ReadUInt16();
                        }

                        newNode.materialID = modelStream.ReadInt32();
                        modelStream.BaseStream.Position += 12;

                        break;
                    }
                #endregion
            }

            if (parentID < 0) { newNode.hmatrix = newNode.tmatrix; }
            else { newNode.hmatrix = matrixMult(newNode.tmatrix, nodeList[parentID].hmatrix); }

            nodeList.Add(newNode);
            int currentID = nodeList.IndexOf(newNode);

            for (int c = 0; c < childrenCount; c++)
            {
                readNodes(modelStream, nodeList, currentID);
            }
        }

        private static float[,] matrixMult(float[,] ma, float[,] mb)
        {
            float[,] mm = new float[4, 4];

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    mm[i, j] = ma[i, 0] * mb[0, j] + ma[i, 1] * mb[1, j] + ma[i, 2] * mb[2, j] + ma[i, 3] * mb[3, j];
                }
            }

            /*
            mm[0, 0] = ma00*mb00 + ma01*mb10 + ma02*mb20 + ma03*mb30
            mm[0, 1] = ma00*mb01 + ma01*mb11 + ma02*mb21 + ma03*mb31
            mm[0, 2] = ma00*mb02 + ma01*mb12 + ma02*mb22 + ma03*mb32
            mm[0, 3] = ma00*mb03 + ma01*mb13 + ma02*mb23 + ma03*mb33

            mm[1, 1] = ma10*mb00 + ma11*mb10 + ma12*mb20 + ma13*mb30
            mm[1, 1] = ma10*mb01 + ma11*mb11 + ma12*mb21 + ma13*mb31
            mm[1, 2] = ma10*mb02 + ma11*mb12 + ma12*mb22 + ma13*mb32
            mm[1, 3] = ma10*mb03 + ma11*mb13 + ma12*mb23 + ma13*mb33
            
            mm[2, 0] = ma20*mb00 + ma21*mb10 + ma22*mb20 + ma23*mb30
            mm[2, 1] = ma20*mb01 + ma21*mb11 + ma22*mb21 + ma23*mb31
            mm[2, 2] = ma20*mb02 + ma21*mb12 + ma22*mb22 + ma23*mb32
            mm[2, 3] = ma20*mb03 + ma21*mb13 + ma22*mb23 + ma23*mb33
            
            mm[3, 0] = ma30*mb00 + ma31*mb10 + ma32*mb20 + ma33*mb30
            mm[3, 1] = ma30*mb01 + ma31*mb11 + ma32*mb21 + ma33*mb31
            mm[3, 2] = ma30*mb02 + ma31*mb12 + ma32*mb22 + ma33*mb32
            mm[3, 3] = ma30*mb03 + ma31*mb13 + ma32*mb23 + ma33*mb33*/

            return mm;
        }

        private static float[] MatrixToEuler(float[,] transf)
        {
            double heading = 0;
            double attitude = 0;
            double bank = 0;
            //original code by Martin John Baker for right-handed coordinate system
            /*if (transf[0, 1] > 0.998)
            { // singularity at north pole
                heading = Math.Atan2(transf[0, 2], transf[2, 2]);
                attitude = Math.PI / 2;
                bank = 0;
            }
            if (transf[0, 1] < -0.998)
            { // singularity at south pole
                heading = Math.Atan2(transf[0, 2], transf[2, 2]);
                attitude = -Math.PI / 2;
                bank = 0;
            }

            heading = Math.Atan2(-transf[2, 0], transf[0, 0]);
            bank = Math.Atan2(-transf[1, 2], transf[1, 1]);
            attitude = Math.Asin(transf[1, 0]);*/

            //left handed
            if (transf[0, 1] > 0.998)
            { // singularity at north pole
                heading = Math.Atan2(-transf[1, 0], transf[1, 1]);
                attitude = -Math.PI / 2;
                bank = 0;
            }
            else if (transf[0, 1] < -0.998)
            { // singularity at south pole
                heading = Math.Atan2(-transf[1, 0], transf[1, 1]);
                attitude = Math.PI / 2;
                bank = 0;
            }
            else
            {
                heading = Math.Atan2(transf[0, 1], transf[0, 0]);
                bank = Math.Atan2(transf[1, 2], transf[2, 2]);
                attitude = Math.Asin(-transf[0, 2]);
            }


            //alternative code by Mike Day, Insomniac Games
            /*bank = Math.Atan2(transf[1, 2], transf[2, 2]);

            double c2 = Math.Sqrt(transf[0, 0] * transf[0, 0] + transf[0, 1] * transf[0, 1]);
            attitude = Math.Atan2(-transf[0, 2], c2);

            double s1 = Math.Sin(bank);
            double c1 = Math.Cos(bank);
            heading = Math.Atan2(s1 * transf[2, 0] - c1 * transf[1, 0], c1 * transf[1, 1] - s1 * transf[2, 1]);*/

            attitude *= 180 / Math.PI;
            heading *= 180 / Math.PI;
            bank *= 180 / Math.PI;

            return new float[3] { (float)bank, (float)attitude, (float)heading };
        }

        private static float[] ScaleFromMatrix(float[,] transf)
        {
            double scaleX = Math.Sqrt(transf[0, 0] * transf[0, 0] + transf[1, 0] * transf[1, 0] + transf[2, 0] * transf[2, 0]);
            double scaleY = Math.Sqrt(transf[0, 1] * transf[0, 1] + transf[1, 1] * transf[1, 1] + transf[2, 1] * transf[2, 1]);
            double scaleZ = Math.Sqrt(transf[0, 2] * transf[0, 2] + transf[1, 2] * transf[1, 2] + transf[2, 2] * transf[2, 2]);

            return new float[3] { (float)scaleX, (float)scaleY, (float)scaleZ };
        }

        private static string ReadStr(BinaryReader str, int len)
        {
            //int len = str.ReadInt32();
            byte[] stringData = new byte[len];
            str.Read(stringData, 0, len);
            var result = System.Text.Encoding.UTF8.GetString(stringData);
            return result;
        }


        private static void ExportOBJ(kn5Model srcModel, bool ZMhack)
        {
            string modelFilename = srcModel.modelName;
            if (ZMhack) { modelFilename += "_ZMhack"; }

            if (!File.Exists(srcModel.modelDir + modelFilename + ".obj"))
            {
                Console.WriteLine("Exporting {0}.obj", modelFilename);

                #region write MTL
                using (StreamWriter MTLwriter = new StreamWriter(File.Create(srcModel.modelDir + modelFilename + ".mtl")))
                {
                    StringBuilder sb = new StringBuilder();

                    foreach (var srcMat in srcModel.materials)
                    {
                        sb.AppendFormat("newmtl {0}\r\n", srcMat.name.Replace(' ', '_'));
                        sb.AppendFormat("Ka {0} {0} {0}\r\n", srcMat.ksAmbient);
                        sb.AppendFormat("Kd {0} {0} {0}\r\n", srcMat.ksDiffuse);
                        sb.AppendFormat("Ks {0} {0} {0}\r\n", srcMat.ksSpecular);
                        sb.AppendFormat("Ns {0}\r\n", srcMat.ksSpecularEXP);
                        sb.AppendFormat("illum 2\r\n", srcMat.ksSpecular);
//add function to search for textures and get relative path
                        if (srcMat.useDetail == 1.0f && srcMat.txDetail != null)
                        {
                            sb.AppendFormat("map_Kd texture\\{0}\r\n", srcMat.txDetail);
                            if (srcMat.txDiffuse != null) { sb.AppendFormat("map_Ks texture\\{0}\r\n", srcMat.txDiffuse); }
                        }
                        else if (srcMat.txDiffuse != null) { sb.AppendFormat("map_Kd texture\\{0}\r\n", srcMat.txDiffuse); }
                        if (srcMat.txNormal != null) { sb.AppendFormat("bump texture\\{0}\r\n", srcMat.txNormal); }
                        sb.Append("\r\n");
                    }

                    MTLwriter.Write(sb);
                }
                #endregion

                #region write OBJ
                using (StreamWriter OBJwriter = new StreamWriter(File.Create(srcModel.modelDir + modelFilename + ".obj")))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("# Assetto Corsa model\r\n# Exported with kn5 Converter by Chipicao on {0}\r\n", DateTime.Now);
                    sb.AppendFormat("\r\nmtllib {0}.mtl\r\n", modelFilename);

                    int vertexPad = 1;

                    foreach (var srcNode in srcModel.nodes)
                    {
                        switch (srcNode.type)
                        {
                            case 1:
                                {
                                    if (ZMhack)
                                    {
                                        //create dummy box
                                        srcNode.vertexCount = 24;
                                        srcNode.position = new float[72] { 0.05f, -0.05f, 0.05f, 0.05f, 0.05f, 0.05f, -0.05f, 0.05f, 0.05f, -0.05f, -0.05f, 0.05f, 0.05f, 0.05f, -0.05f, 0.05f, 0.05f, 0.05f, 0.05f, -0.05f, 0.05f, 0.05f, -0.05f, -0.05f, -0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, -0.05f, -0.05f, 0.05f, -0.05f, -0.05f, 0.05f, -0.05f, 0.05f, 0.05f, -0.05f, 0.05f, -0.05f, -0.05f, -0.05f, -0.05f, -0.05f, -0.05f, -0.05f, 0.05f, -0.05f, 0.05f, 0.05f, -0.05f, 0.05f, -0.05f, -0.05f, -0.05f, -0.05f, 0.05f, -0.05f, -0.05f, 0.05f, -0.05f, 0.05f, -0.05f, -0.05f, 0.05f, -0.05f, -0.05f, -0.05f };
                                        srcNode.normal = new float[72] { -0f, 0f, 1f, -0f, 0f, 1f, -0f, 0f, 1f, -0f, 0f, 1f, 1f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 0f, -0f, 1f, 0f, -0f, 1f, 0f, -0f, 1f, 0f, -0f, 1f, 0f, -0f, 0f, -1f, -0f, 0f, -1f, -0f, 0f, -1f, -0f, 0f, -1f, -1f, 0f, 0f, -1f, 0f, 0f, -1f, 0f, 0f, -1f, 0f, 0f, -0f, -1f, 0f, -0f, -1f, 0f, -0f, -1f, 0f, -0f, -1f, 0f };
                                        srcNode.texture0 = new float[48] { 0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f, 0f, 0f };
                                        srcNode.indices = new ushort[36] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7, 8, 9, 10, 8, 10, 11, 12, 13, 14, 12, 14, 15, 16, 17, 18, 16, 18, 19, 20, 21, 22, 20, 22, 23 };
                                        goto case 3;
                                    }
                                    break;
                                }
                            case 2:
                            case 3:
                                {
                                    sb.AppendFormat("\r\ng {0}", srcNode.name.Replace(' ', '_'));
                                    if (ZMhack && srcNode.parentID >= 0) { sb.AppendFormat(" {0}", srcModel.nodes[srcNode.parentID].name.Replace(' ', '_')); }
                                    sb.AppendFormat("\r\n");

                                    for (int v = 0; v < srcNode.vertexCount; v++)
                                    {
                                        var x = srcNode.position[v * 3];
                                        var y = srcNode.position[v * 3 + 1];
                                        var z = srcNode.position[v * 3 + 2];

                                        float vx = srcNode.hmatrix[0, 0] * x + srcNode.hmatrix[1, 0] * y + srcNode.hmatrix[2, 0] * z + srcNode.hmatrix[3, 0];
                                        float vy = srcNode.hmatrix[0, 1] * x + srcNode.hmatrix[1, 1] * y + srcNode.hmatrix[2, 1] * z + srcNode.hmatrix[3, 1];
                                        float vz = srcNode.hmatrix[0, 2] * x + srcNode.hmatrix[1, 2] * y + srcNode.hmatrix[2, 2] * z + srcNode.hmatrix[3, 2];

                                        sb.AppendFormat("v {0} {1} {2}\r\n", vx, vy, vz);
                                    }
                                    OBJwriter.Write(sb);
                                    sb.Length = 0;

                                    for (int v = 0; v < srcNode.vertexCount; v++)
                                    {
                                        var x = srcNode.normal[v * 3];
                                        var y = srcNode.normal[v * 3 + 1];
                                        var z = srcNode.normal[v * 3 + 2];

                                        //transforming normal vectors requires the transposed inverse matrix
                                        //transformation matrices SHOULD be normalized (assumption, mother of all fuckups)
                                        //so in theory, the inverse of a normalized matrix is the transposed matrix, which transposed again is the matrix itself
                                        float nx = srcNode.hmatrix[0, 0] * x + srcNode.hmatrix[1, 0] * y + srcNode.hmatrix[2, 0] * z;
                                        float ny = srcNode.hmatrix[0, 1] * x + srcNode.hmatrix[1, 1] * y + srcNode.hmatrix[2, 1] * z;
                                        float nz = srcNode.hmatrix[0, 2] * x + srcNode.hmatrix[1, 2] * y + srcNode.hmatrix[2, 2] * z;

                                        sb.AppendFormat("vn {0} {1} {2}\r\n", nx, ny, nz);
                                    }
                                    OBJwriter.Write(sb);
                                    sb.Length = 0;

                                    float UVmult = 1.0f;
                                    if (srcNode.materialID >= 0)
                                    {
                                        if (srcModel.materials[srcNode.materialID].useDetail == 0.0f) { UVmult = srcModel.materials[srcNode.materialID].diffuseMult; }
                                        else { UVmult = srcModel.materials[srcNode.materialID].detailUVMultiplier; }
                                    }

                                    for (int v = 0; v < srcNode.vertexCount; v++)
                                    {
                                        var tx = srcNode.texture0[v * 2] * UVmult;
                                        var ty = srcNode.texture0[v * 2 + 1] * UVmult;

                                        sb.AppendFormat("vt {0} {1}\r\n", tx, ty);
                                    }
                                    OBJwriter.Write(sb);
                                    sb.Length = 0;

                                    if (srcNode.materialID >= 0) { sb.AppendFormat("\r\nusemtl {0}\r\n", srcModel.materials[srcNode.materialID].name.Replace(' ', '_')); }
                                    else { sb.AppendFormat("\r\nusemtl Default\r\n"); }
                                    for (int i = 0; i < srcNode.indices.Length / 3; i++)
                                    {
                                        var i1 = srcNode.indices[i * 3] + vertexPad;
                                        var i2 = srcNode.indices[i * 3 + 1] + vertexPad;
                                        var i3 = srcNode.indices[i * 3 + 2] + vertexPad;

                                        sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\r\n", i1, i2, i3);
                                    }
                                    OBJwriter.Write(sb);
                                    sb.Length = 0;

                                    vertexPad += srcNode.vertexCount;
                                    break;
                                }
                        }
                    }
                }
                #endregion
            }
            else { Console.WriteLine("File already exists: {0}.obj", modelFilename); }
        }

        private static void ExportFBX(kn5Model srcModel)
        {
            if (!File.Exists(srcModel.modelDir + srcModel.modelName + ".fbx"))
            {
                Console.WriteLine("Exporting {0}.fbx", srcModel.modelName);

                using (StreamWriter FBXwriter = new StreamWriter(File.Create(srcModel.modelDir + srcModel.modelName + ".fbx")))
                {
                    StringBuilder fbx = new StringBuilder();
                    var timestamp = DateTime.Now;

                    StringBuilder ob = new StringBuilder(); //objects builder
                    //ob.Append("\nObjects:  {");

                    StringBuilder cb = new StringBuilder(); //connections builder
                    cb.Append("\n}\n");//Objects end
                    cb.Append("\nConnections:  {");

                    int FBXgeometryCount = 0;
                    foreach (var srcNode in srcModel.nodes.Where(n => n.type > 1)) { FBXgeometryCount++; }

                    #region build materials first to get used texture count
                    for (int m = 0; m < srcModel.materials.Count; m++)
                    {
                        var srcMat = srcModel.materials[m];

                        ob.Append("\n\tMaterial: " + (400000 + m) + ", \"Material::" + srcMat.name + "\", \"\" {");
                        ob.Append("\n\t\tVersion: 102");
                        ob.Append("\n\t\tShadingModel: \"phong\"");
                        ob.Append("\n\t\tMultiLayer: 0");
                        ob.Append("\n\t\tProperties70:  {");
                        ob.Append("\n\t\t\tP: \"ShadingModel\", \"KString\", \"\", \"\", \"phong\"");
                        ob.AppendFormat("\n\t\t\tP: \"AmbientColor\", \"Color\", \"\", \"A\",{0},{0},{0}", srcMat.ksAmbient);
                        ob.AppendFormat("\n\t\t\tP: \"DiffuseColor\", \"Color\", \"\", \"A\",{0},{0},{0}", srcMat.ksDiffuse);
                        ob.AppendFormat("\n\t\t\tP: \"SpecularColor\", \"Color\", \"\", \"A\",{0},{0},{0}", srcMat.ksSpecular);
                        ob.AppendFormat("\n\t\t\tP: \"SpecularFactor\", \"Number\", \"\", \"A\",{0}", srcMat.ksSpecularEXP / 100f);
                        //ob.AppendFormat("\n\t\t\tP: \"ShininessExponent\", \"Number\", \"\", \"A\",{0}", srcMat.ksSpecularEXP);
                        //ob.AppendFormat("\n\t\t\tP: \"Shininess\", \"double\", \"Number\", \"\",{0}", srcMat.ksSpecularEXP);
                        ob.Append("\n\t\t}");
                        ob.Append("\n\t}");

                        //try to provide some level of texture instancing by using a custom class to match file and UV scaling

                        int txDiffuseID = srcModel.usedTex.FindIndex(s => s.filename == srcMat.txDiffuse && s.UVScaling == srcMat.diffuseMult);
                        if (txDiffuseID < 0 && srcMat.txDiffuse != null) //add texture to instance list
                        {
                            srcModel.usedTex.Add(new kn5Texture() { filename = srcMat.txDiffuse, UVScaling = srcMat.diffuseMult });
                            txDiffuseID = srcModel.usedTex.Count - 1;
                        }

                        if (srcMat.useDetail == 1.0f && srcMat.txDetail != null)
                        {
                            int txDetailID = srcModel.usedTex.FindIndex(s => s.filename == srcMat.txDetail && s.UVScaling == srcMat.detailUVMultiplier);
                            if (txDetailID < 0)
                            {
                                srcModel.usedTex.Add(new kn5Texture() { filename = srcMat.txDetail, UVScaling = srcMat.detailUVMultiplier });
                                txDetailID = srcModel.usedTex.Count - 1;
                            }

                            cb.Append("\n\tC: \"OP\"," + (500000 + txDetailID) + "," + (400000 + m) + ", \"DiffuseColor\"");

                            //use txDiffuse(AO) as specular map
                            cb.Append("\n\tC: \"OP\"," + (500000 + txDiffuseID) + "," + (400000 + m) + ", \"SpecularColor\"");
                        }
                        else
                        {
                            cb.Append("\n\tC: \"OP\"," + (500000 + txDiffuseID) + "," + (400000 + m) + ", \"DiffuseColor\"");

                            cb.Append("\n\tC: \"OP\"," + (500000 + txDiffuseID) + "," + (400000 + m) + ", \"TransparentColor\"");
                        }

                        if (srcMat.txNormal != null)
                        {
                            int txNormalID = srcModel.usedTex.FindIndex(s => s.filename == srcMat.txNormal && s.UVScaling == srcMat.normalMult);
                            if (txNormalID < 0)
                            {
                                srcModel.usedTex.Add(new kn5Texture() { filename = srcMat.txNormal, UVScaling = srcMat.normalMult });
                                txNormalID = srcModel.usedTex.Count - 1;
                            }

                            cb.Append("\n\tC: \"OP\"," + (500000 + txNormalID) + "," + (400000 + m) + ", \"NormalMap\"");
                        }
                    }
                    #endregion

                    #region write generic FBX data
                    fbx.Append("; FBX 7.1.0 project file");
                    fbx.Append("\nFBXHeaderExtension:  {\n\tFBXHeaderVersion: 1003\n\tFBXVersion: 7100\n\tCreationTimeStamp:  {\n\t\tVersion: 1000");
                    fbx.Append("\n\t\tYear: " + timestamp.Year);
                    fbx.Append("\n\t\tMonth: " + timestamp.Month);
                    fbx.Append("\n\t\tDay: " + timestamp.Day);
                    fbx.Append("\n\t\tHour: " + timestamp.Hour);
                    fbx.Append("\n\t\tMinute: " + timestamp.Minute);
                    fbx.Append("\n\t\tSecond: " + timestamp.Second);
                    fbx.Append("\n\t\tMillisecond: " + timestamp.Millisecond);
                    fbx.Append("\n\t}\n\tCreator: \"kn5 converter by Chipicao\"\n}\n");

                    fbx.Append("\nGlobalSettings:  {");
                    fbx.Append("\n\tVersion: 1000");
                    fbx.Append("\n\tProperties70:  {");
                    fbx.Append("\n\t\tP: \"UpAxis\", \"int\", \"Integer\", \"\",1");
                    fbx.Append("\n\t\tP: \"UpAxisSign\", \"int\", \"Integer\", \"\",1");
                    fbx.Append("\n\t\tP: \"FrontAxis\", \"int\", \"Integer\", \"\",2");
                    fbx.Append("\n\t\tP: \"FrontAxisSign\", \"int\", \"Integer\", \"\",1");
                    fbx.Append("\n\t\tP: \"CoordAxis\", \"int\", \"Integer\", \"\",0");
                    fbx.Append("\n\t\tP: \"CoordAxisSign\", \"int\", \"Integer\", \"\",1");
                    fbx.Append("\n\t\tP: \"OriginalUpAxis\", \"int\", \"Integer\", \"\",1");
                    fbx.Append("\n\t\tP: \"OriginalUpAxisSign\", \"int\", \"Integer\", \"\",1");
                    fbx.Append("\n\t\tP: \"UnitScaleFactor\", \"double\", \"Number\", \"\",1");
                    fbx.Append("\n\t\tP: \"OriginalUnitScaleFactor\", \"double\", \"Number\", \"\",1");
                    //sb.Append("\n\t\tP: \"AmbientColor\", \"ColorRGB\", \"Color\", \"\",0,0,0");
                    //sb.Append("\n\t\tP: \"DefaultCamera\", \"KString\", \"\", \"\", \"Producer Perspective\"");
                    //sb.Append("\n\t\tP: \"TimeMode\", \"enum\", \"\", \"\",6");
                    //sb.Append("\n\t\tP: \"TimeProtocol\", \"enum\", \"\", \"\",2");
                    //sb.Append("\n\t\tP: \"SnapOnFrameMode\", \"enum\", \"\", \"\",0");
                    //sb.Append("\n\t\tP: \"TimeSpanStart\", \"KTime\", \"Time\", \"\",0");
                    //sb.Append("\n\t\tP: \"TimeSpanStop\", \"KTime\", \"Time\", \"\",153953860000");
                    //sb.Append("\n\t\tP: \"CustomFrameRate\", \"double\", \"Number\", \"\",-1");
                    //sb.Append("\n\t\tP: \"TimeMarker\", \"Compound\", \"\", \"\"");
                    //sb.Append("\n\t\tP: \"CurrentTimeMarker\", \"int\", \"Integer\", \"\",-1");
                    fbx.Append("\n\t}\n}\n");

                    fbx.Append("\nDocuments:  {");
                    fbx.Append("\n\tCount: 1");
                    fbx.Append("\n\tDocument: 1234567890, \"\", \"Scene\" {");
                    fbx.Append("\n\t\tProperties70:  {");
                    fbx.Append("\n\t\t\tP: \"SourceObject\", \"object\", \"\", \"\"");
                    fbx.Append("\n\t\t\tP: \"ActiveAnimStackName\", \"KString\", \"\", \"\", \"\"");
                    fbx.Append("\n\t\t}");
                    fbx.Append("\n\t\tRootNode: 0");
                    fbx.Append("\n\t}\n}\n");
                    fbx.Append("\nReferences:  {\n}\n");

                    fbx.Append("\nDefinitions:  {");
                    fbx.Append("\n\tVersion: 100");
                    fbx.AppendFormat("\n\tCount: {0}", 1 + srcModel.nodes.Count + FBXgeometryCount + srcModel.materials.Count + srcModel.usedTex.Count * 2);

                    fbx.Append("\n\tObjectType: \"GlobalSettings\" {");
                    fbx.Append("\n\t\tCount: 1");
                    fbx.Append("\n\t}");

                    fbx.Append("\n\tObjectType: \"Model\" {");
                    fbx.Append("\n\t\tCount: " + srcModel.nodes.Count);
                    fbx.Append("\n\t}");

                    fbx.Append("\n\tObjectType: \"Geometry\" {");
                    fbx.Append("\n\t\tCount: " + FBXgeometryCount);
                    fbx.Append("\n\t}");

                    fbx.Append("\n\tObjectType: \"Material\" {");
                    fbx.Append("\n\t\tCount: " + srcModel.materials.Count);
                    fbx.Append("\n\t}");

                    fbx.Append("\n\tObjectType: \"Texture\" {");
                    fbx.Append("\n\t\tCount: " + srcModel.usedTex.Count);
                    fbx.Append("\n\t}");

                    fbx.Append("\n\tObjectType: \"Video\" {");
                    fbx.Append("\n\t\tCount: " + srcModel.usedTex.Count);
                    fbx.Append("\n\t}");
                    fbx.Append("\n}\n");
                    fbx.Append("\nObjects:  {");

                    FBXwriter.Write(fbx);
                    fbx.Length = 0;
                    //write previously built materials
                    FBXwriter.Write(ob);
                    ob.Length = 0;
                    #endregion

                    #region write Texture & Video data
                    for (int t = 0; t < srcModel.usedTex.Count; t++)
                    {
                        string textureName = srcModel.usedTex[t].filename;
                        string textureFile = srcModel.modelDir + "texture\\" + textureName;
                        string relativePath = "texture\\" + textureName;

                        //search for texture if doesn't exist
                        //later

                        ob.Append("\n\tTexture: " + (500000 + t) + ", \"Texture::" + textureName + "\", \"\" {");
                        ob.Append("\n\t\tType: \"TextureVideoClip\"");
                        ob.Append("\n\t\tVersion: 202");
                        ob.Append("\n\t\tTextureName: \"Texture::" + textureName + "\"");
                        ob.Append("\n\t\tProperties70:  {");
                        ob.AppendFormat("\n\t\t\tP: \"Translation\", \"Vector\", \"\", \"A\",{0},{0},1", 0.5f * (1 - srcModel.usedTex[t].UVScaling));
                        ob.AppendFormat("\n\t\t\tP: \"Scaling\", \"Vector\", \"\", \"A\",{0},{0},1", srcModel.usedTex[t].UVScaling);
                        ob.Append("\n\t\t\tP: \"UVSet\", \"KString\", \"\", \"\", \"UVChannel_1\"");
                        ob.Append("\n\t\t\tP: \"UseMaterial\", \"bool\", \"\", \"\",1");
                        ob.Append("\n\t\t}");
                        ob.Append("\n\t\tMedia: \"Video::" + textureName + "\"");
                        ob.Append("\n\t\tFileName: \"" + textureFile + "\"");
                        ob.Append("\n\t\tRelativeFilename: \"" + relativePath + "\"");
                        ob.Append("\n\t\tTexture_Alpha_Source: \"Alpha_Black\"");
                        ob.Append("\n\t}");

                        ob.Append("\n\tVideo: " + (600000 + t) + ", \"Video::" + textureName + "\", \"Clip\" {");
                        ob.Append("\n\t\tType: \"Clip\"");
                        ob.Append("\n\t\tProperties70:  {");
                        ob.Append("\n\t\t\tP: \"Path\", \"KString\", \"XRefUrl\", \"\", \"" + textureFile + "\"");
                        ob.Append("\n\t\t}");
                        ob.Append("\n\t\tFileName: \"" + textureFile + "\"");
                        ob.Append("\n\t\tRelativeFilename: \"" + relativePath + "\"");
                        ob.Append("\n\t}");

                        //connect video to texture
                        cb.Append("\n\tC: \"OO\"," + (600000 + t) + "," + (500000 + t));
                    }

                    FBXwriter.Write(ob);
                    ob.Length = 0;
                    #endregion

                    #region write Model & Geometry data
                    for (int n = 0; n < srcModel.nodes.Count; n++)
                    {
                        var srcNode = srcModel.nodes[n];

                        #region if Mesh node
                        if (srcNode.type > 1)
                        {
                            StringBuilder vb = new StringBuilder();
                            StringBuilder ib = new StringBuilder();

                            //write Geometry
                            ob.Append("\n\tGeometry: " + (100000 + n) + ", \"Geometry::\", \"Mesh\" {");
                            ob.Append("\n\t\tProperties70:  {");
                            var randomColor = RandomColorGenerator((100000 + n).ToString());
                            ob.AppendFormat("\n\t\t\tP: \"Color\", \"ColorRGB\", \"Color\", \"\",{0},{1},{2}", ((float)randomColor[0] / 255), ((float)randomColor[1] / 255), ((float)randomColor[2] / 255));
                            ob.Append("\n\t\t}");

                            ob.AppendFormat("\n\t\tVertices: *{0} {{\n\t\t\ta: ", (srcNode.vertexCount * 3));
                            foreach (var v in srcNode.position) { vb.AppendFormat("{0},", v); }
                            vb.Length -= 1; //remove last ,
                            ob.Append(SplitLine(vb.ToString()));
                            ob.Append("\n\t\t}");
                            vb.Length = 0;

                            ob.AppendFormat("\n\t\tPolygonVertexIndex: *{0} {{\n\t\t\ta: ", srcNode.indices.Length);
                            for (int f = 0; f < (srcNode.indices.Length / 3); f++)
                            {
                                ib.Append(srcNode.indices[f * 3]);
                                ib.Append(',');
                                ib.Append(srcNode.indices[f * 3 + 1]);
                                ib.Append(',');
                                ib.Append(-1 - srcNode.indices[f * 3 + 2]);
                                ib.Append(',');
                            }
                            ib.Length -= 1; //remove last ,
                            ob.Append(SplitLine(ib.ToString()));
                            ob.Append("\n\t\t}");
                            ib.Length = 0;
                            ob.Append("\n\t\tGeometryVersion: 124");

                            ob.Append("\n\t\tLayerElementNormal: 0 {");
                            ob.Append("\n\t\t\tVersion: 101");
                            ob.Append("\n\t\t\tName: \"\"");
                            ob.Append("\n\t\t\tMappingInformationType: \"ByVertice\"");
                            ob.Append("\n\t\t\tReferenceInformationType: \"Direct\"");
                            ob.AppendFormat("\n\t\t\tNormals: *{0} {{\n\t\t\ta: ", (srcNode.vertexCount * 3));
                            foreach (var v in srcNode.normal) { vb.AppendFormat("{0},", v); }
                            vb.Length -= 1; //remove last ,
                            ob.Append(SplitLine(vb.ToString()));
                            ob.Append("\n\t\t\t}\n\t\t}");
                            vb.Length = 0;

                            ob.Append("\n\t\tLayerElementUV: 0 {");
                            ob.Append("\n\t\t\tVersion: 101");
                            ob.Append("\n\t\t\tName: \"UVChannel_0\"");
                            ob.Append("\n\t\t\tMappingInformationType: \"ByVertice\"");
                            ob.Append("\n\t\t\tReferenceInformationType: \"Direct\"");
                            ob.AppendFormat("\n\t\t\tUV: *{0} {{\n\t\t\ta: ", (srcNode.vertexCount * 2));
                            foreach (var v in srcNode.texture0) { vb.AppendFormat("{0},", v); }
                            vb.Length -= 1; //remove last ,
                            ob.Append(SplitLine(vb.ToString()));
                            ob.Append("\n\t\t\t}\n\t\t}");
                            vb.Length = 0;

                            ob.Append("\n\t\tLayerElementMaterial: 0 {");
                            ob.Append("\n\t\t\tVersion: 101");
                            ob.Append("\n\t\t\tName: \"\"");
                            ob.Append("\n\t\t\tMappingInformationType: \"AllSame\"");
                            ob.Append("\n\t\t\tReferenceInformationType: \"IndexToDirect\"");
                            ob.Append("\n\t\t\tMaterials: *1 {");
                            ob.Append("\n\t\t\t\t0");
                            ob.Append("\n\t\t\t}");
                            ob.Append("\n\t\t}");

                            ob.Append("\n\t\tLayer: 0 {");
                            ob.Append("\n\t\t\tVersion: 100");
                            ob.Append("\n\t\t\tLayerElement:  {");
                            ob.Append("\n\t\t\t\tType: \"LayerElementNormal\"");
                            ob.Append("\n\t\t\t\tTypedIndex: 0");
                            ob.Append("\n\t\t\t}");
                            ob.Append("\n\t\t\tLayerElement:  {");
                            ob.Append("\n\t\t\t\tType: \"LayerElementMaterial\"");
                            ob.Append("\n\t\t\t\tTypedIndex: 0");
                            ob.Append("\n\t\t\t}");
                            ob.Append("\n\t\t\tLayerElement:  {");
                            ob.Append("\n\t\t\t\tType: \"LayerElementTexture\"");
                            ob.Append("\n\t\t\t\tTypedIndex: 0");
                            ob.Append("\n\t\t\t}");
                            ob.Append("\n\t\t\tLayerElement:  {");
                            ob.Append("\n\t\t\t\tType: \"LayerElementBumpTextures\"");
                            ob.Append("\n\t\t\t\tTypedIndex: 0");
                            ob.Append("\n\t\t\t}");
                            ob.Append("\n\t\t\tLayerElement:  {");
                            ob.Append("\n\t\t\t\tType: \"LayerElementUV\"");
                            ob.Append("\n\t\t\t\tTypedIndex: 0");
                            ob.Append("\n\t\t\t}");
                            ob.Append("\n\t\t}"); //Layer 0 end
                            ob.Append("\n\t}"); //Geometry end

                            //connect Geometry to Model
                            cb.Append("\n\tC: \"OO\"," + (100000 + n) + "," + (200000 + n));
                            //connect Material to Model
                            if (srcNode.materialID > -1) { cb.Append("\n\tC: \"OO\"," + (400000 + srcNode.materialID) + "," + (200000 + n)); }

                            ob.Append("\n\tModel: " + (200000 + n) + ", \"Model::" + srcNode.name + "\", \"Mesh\" {");
                        }
                        #endregion
                        else { ob.Append("\n\tModel: " + (200000 + n) + ", \"Model::" + srcNode.name + "\", \"Null\" {"); }

                        ob.Append("\n\t\tVersion: 232");
                        ob.Append("\n\t\tProperties70:  {");
                        ob.Append("\n\t\t\tP: \"InheritType\", \"enum\", \"\", \"\",1");
                        ob.Append("\n\t\t\tP: \"ScalingMax\", \"Vector3D\", \"Vector\", \"\",0,0,0");
                        ob.Append("\n\t\t\tP: \"DefaultAttributeIndex\", \"int\", \"Integer\", \"\",0");
                        ob.Append("\n\t\t\tP: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\"," + srcNode.translation[0] + "," + srcNode.translation[1] + "," + srcNode.translation[2]);
                        ob.Append("\n\t\t\tP: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A\"," + srcNode.rotation[0] + "," + srcNode.rotation[1] + "," + srcNode.rotation[2]);
                        ob.Append("\n\t\t\tP: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\"," + srcNode.scaling[0] + "," + srcNode.scaling[1] + "," + srcNode.scaling[2]);
                        //ob.Append("\n\t\t\tP: \"UDP3DSMAX\", \"KString\", \"\", \"U\", \"Exported_with = kn5 converter by Chipicao&cr;&lf;\"");
                        if (srcNode.type > 1 && srcNode.materialID > -1)
                        {
                            var srcMat = srcModel.materials[srcNode.materialID];
                            /*ob.Append("\n\t\t\tP: \"UDP3DSMAX\", \"KString\", \"\", \"U\", \"");
                            ob.AppendFormat("diffuseMult = {0}&cr;&lf;", srcMat.diffuseMult);
                            ob.AppendFormat("normalMult = {0}&cr;&lf;", srcMat.normalMult);
                            ob.AppendFormat("useDetail = {0}&cr;&lf;", srcMat.useDetail);
                            ob.AppendFormat("detailUVMultiplier = {0}&cr;&lf;", srcMat.detailUVMultiplier);
                            ob.Append("\"");*/
                            ob.AppendFormat("\n\t\t\tP: \"UDP3DSMAX\", \"KString\", \"\", \"U\", \"{0}\"", srcMat.shaderProps);
                        }
                        //ob.Append("\n\t\t\tP: \"MaxHandle\", \"int\", \"Integer\", \"UH\"," + (j + 2 + pmodel.nodeList.Count));
                        ob.Append("\n\t\t}");
                        ob.Append("\n\t\tShading: T");
                        ob.Append("\n\t\tCulling: \"CullingOff\"");
                        ob.Append("\n\t}"); //Model end

                        //connect Model to parent
                        if (srcNode.parentID < 0) { cb.Append("\n\tC: \"OO\"," + (200000 + n) + ",0"); }
                        else { cb.Append("\n\tC: \"OO\"," + (200000 + n) + "," + (200000 + srcNode.parentID)); }

                        FBXwriter.Write(ob);
                        ob.Length = 0;
                    }
                    #endregion

                    cb.Append("\n}");//Connections end
                    FBXwriter.Write(cb);
                }
            }
            else { Console.WriteLine("File already exists: {0}.fbx", srcModel.modelName); }
        }

        private static void ExportIni(kn5Model srcModel)
        {
            if (!File.Exists(srcModel.modelDir + srcModel.modelName + ".fbx.ini"))
            {
                Console.WriteLine("Exporting {0}.fbx.ini", srcModel.modelName);

                using (StreamWriter INIWriter = new StreamWriter(File.Create(srcModel.modelDir + srcModel.modelName + ".fbx.ini")))
                {

                    INIWriter.WriteLine("[HEADER]");
                    INIWriter.WriteLine("VERSION = 3");
                    INIWriter.WriteLine("");
                    INIWriter.WriteLine("[MATERIAL_LIST]");
                    INIWriter.WriteLine("COUNT=" + srcModel.materials.Count);
                    INIWriter.WriteLine("");

                    for (int i = 0; i < srcModel.materials.Count; i++)
                    {
                        INIWriter.WriteLine("[MATERIAL_" + i + "]");
                        INIWriter.WriteLine("NAME=" + srcModel.materials[i].name);
                        INIWriter.WriteLine("SHADER=" + srcModel.materials[i].shader);
                        INIWriter.WriteLine("ALPHABLEND=" + srcModel.materials[i].alphaBlend);
                        INIWriter.WriteLine("APLHATEST=" + srcModel.materials[i].alphaTest);
                        INIWriter.WriteLine("DEPTHMODE=" + srcModel.materials[i].depthMode);
                        INIWriter.WriteLine("VARCOUNT=" + srcModel.materials[i].var.Count);
                        for(int j = 0; j < srcModel.materials[i].var.Count; j++)
                        {
                            INIWriter.WriteLine("VAR_" + j + "_NAME=" + srcModel.materials[i].var[j].name);
                            INIWriter.WriteLine("VAR_" + j + "_FLOAT1=" + srcModel.materials[i].var[j].value.ToString("0.0"));
                            INIWriter.WriteLine("VAR_" + j + "_FLOAT2=0.0,0.0");
                            INIWriter.WriteLine("VAR_" + j + "_FLOAT3=0,0,0");
                            INIWriter.WriteLine("VAR_" + j + "_FLOAT4=0,0,0,0");
                        }
                        INIWriter.WriteLine("RESCOUNT=" + srcModel.materials[i].res.Count);
                        for(int j = 0; j < srcModel.materials[i].res.Count; j++)
                        {
                            INIWriter.WriteLine("RES_" + j + "_NAME=" + srcModel.materials[i].res[j].name);
                            INIWriter.WriteLine("RES_" + j + "_SLOT=" + srcModel.materials[i].res[j].slot);
                            INIWriter.WriteLine("RES_" + j + "_TEXTURE=" + srcModel.materials[i].res[j].texture);
                        }
                        INIWriter.WriteLine("");
                    }
                }

            }
            else { Console.WriteLine("File already exists: {0}.fbx.ini", srcModel.modelName); }
        }

        private static byte[] RandomColorGenerator(string name)
        {
            int nameHash = name.GetHashCode();
            Random r = new Random(nameHash);
            //Random r = new Random(DateTime.Now.Millisecond);

            byte red = (byte)r.Next(0, 255);
            byte green = (byte)r.Next(0, 255);
            byte blue = (byte)r.Next(0, 255);

            return new byte[3] { red, green, blue };
        }

        private static string SplitLine(string inputLine) //for FBX 2011
        {
            string outputLines = inputLine;
            int vbSplit = 0;
            for (int v = 0; v < inputLine.Length / 2048; v++)
            {
                vbSplit += 2048;
                if (vbSplit < outputLines.Length)
                {
                    vbSplit = outputLines.IndexOf(",", vbSplit) + 1;
                    if (vbSplit > 0) { outputLines = outputLines.Insert(vbSplit, "\n"); }
                }
            }
            return outputLines;
        }

        private static string MakeRelative(string filePath, string referencePath)
        {
            if (filePath != "" && referencePath != "")
            {
                var fileUri = new Uri(filePath);
                var referenceUri = new Uri(referencePath);
                return referenceUri.MakeRelativeUri(fileUri).ToString().Replace('/', Path.DirectorySeparatorChar);
            }
            else
            {
                return "";
            }
        }

    }
}
