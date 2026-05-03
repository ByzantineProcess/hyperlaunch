package dev.byzann.hyperlaunch.agent

import java.lang.instrument.Instrumentation

fun premain(agentArgs: String?, inst: Instrumentation) {
    val pipeName = if (agentArgs != null)
        agentArgs
    else
        "\\\\.\\pipe\\launcher-default"
    AgentBridge.initialise(pipeName)
    inst.addTransformer(GlfwIntercept(), true)
}
