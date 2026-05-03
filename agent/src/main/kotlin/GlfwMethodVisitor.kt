package dev.byzann.hyperlaunch.agent

import org.objectweb.asm.MethodVisitor
import org.objectweb.asm.Opcodes
import org.objectweb.asm.commons.AdviceAdapter

class GlfwMethodVisitor(mv: MethodVisitor, access: Int, name: String, descriptor: String, private val className: String):
    AdviceAdapter(Opcodes.ASM9, mv, access, name, descriptor) {

    override fun onMethodEnter() {
        super.visitLdcInsn(className)
        super.visitMethodInsn(
            INVOKESTATIC,
            "dev/byzann/hyperlaunch/agent/AgentBridge",
            "onWindowCreationPoint",
            "(Ljava/lang/String;)V",
            false
        )
    }
}