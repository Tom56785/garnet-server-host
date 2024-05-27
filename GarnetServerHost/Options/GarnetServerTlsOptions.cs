namespace GarnetServerHost.Options;

public class GarnetServerTlsOptions
{
	public const string ConfigBinding = "GarnetTls";
	public bool SetTlsOptions { get; set; }
	public string CertFileName { get; set; }
	public string CertFilePassword { get; set; }
	public bool ClientCertificateRequired { get; set; }
	public string IssuerCertificatePath { get; set; }
	public string CertSubjectName { get; set; }
	public int CertificateRefreshFrequency { get; set; }
	public bool EnableCluster { get; set; }
	public string ClusterTlsClientTargetHost { get; set; }
}