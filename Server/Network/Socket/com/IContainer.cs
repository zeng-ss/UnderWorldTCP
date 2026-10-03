public interface IContainer
{
    void OnInit();

    void OnServerCommand(ServerBase serverBase, BasePackage basePackage);

    void OnClientCommand(ServerBase serverBase, BasePackage basePackage);
}