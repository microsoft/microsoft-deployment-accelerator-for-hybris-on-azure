package hybrisOnAzure;

import javax.management.InstanceNotFoundException;
import javax.management.MBeanException;
import javax.management.MBeanServerConnection;
import javax.management.MalformedObjectNameException;
import javax.management.ObjectName;
import javax.management.ReflectionException;
import javax.management.remote.JMXConnector;
import javax.management.remote.JMXConnectorFactory;
import javax.management.remote.JMXServiceURL;
import javax.naming.Context;
import java.io.IOException;
import java.util.HashMap;
import java.util.Map;
import java.util.Set;

public class StopWrapper {

    public static void main(String[] args) {
        String objectName = "org.tanukisoftware.wrapper:type=WrapperManager";
        String url = "localhost:9003";

        if (args != null && args.length > 0) {
            url = args[0];
        }

        try {
            JMXConnector jmxConnector = getJMXConnector(url);
            MBeanServerConnection mBeanServerConn = jmxConnector.getMBeanServerConnection();
            Set<ObjectName> objectNames = mBeanServerConn.queryNames(new ObjectName(objectName), null);
            for (final ObjectName name : objectNames) {
                if (name.getCanonicalName().equals(objectName)) {
                    System.out.println("Stopping TanukiWrapper for " + url);
                    mBeanServerConn.invoke(name, "stop", new Object[]{0}, new String[]{int.class.getName()});
                    System.out.println("Successfully stopped TanukiWrapper for " + url);
                    break;
                }
            }
        } catch (InstanceNotFoundException e) {
            e.printStackTrace();
        } catch (MBeanException e) {
            e.printStackTrace();
        } catch (ReflectionException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        } catch (MalformedObjectNameException e) {
            e.printStackTrace();
        } catch (Exception e) {
            e.printStackTrace();
        }

    }

    // JMX-Connection
    static JMXConnector getJMXConnector(String url) throws Exception {
        return getJMXConnector(url, null, null);
    }

    // JMX-Connection
    static JMXConnector getJMXConnector(String url, String usr, String pwd) throws Exception {
        String serviceUrl = "service:jmx:rmi:///jndi/rmi://" + url + "/jmxrmi";
        if (usr == null || usr.trim().length() <= 0 || pwd == null || pwd.trim().length() <= 0) {
            return JMXConnectorFactory.connect(new JMXServiceURL(serviceUrl));
        }
        Map<String, Object> envMap = new HashMap<String, Object>();
        envMap.put("jmx.remote.credentials", new String[]{usr, pwd});
        envMap.put(Context.SECURITY_PRINCIPAL, usr);
        envMap.put(Context.SECURITY_CREDENTIALS, pwd);
        return JMXConnectorFactory.connect(new JMXServiceURL(serviceUrl), envMap);
    }
}
