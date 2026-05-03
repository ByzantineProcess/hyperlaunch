import org.gradle.jvm.tasks.Jar

plugins {
    kotlin("jvm") version "2.2.20"
}

group = "dev.byzann.hyperlaunch"
version = "1.0-SNAPSHOT"

repositories {
    mavenCentral()
}

kotlin {
    jvmToolchain(8)
}

dependencies {
    implementation("org.ow2.asm:asm:9.6")
    implementation("org.ow2.asm:asm-commons:9.6")
}

// Configure the JAR task using Kotlin DSL
tasks.named<Jar>("jar") {
    manifest {
        attributes(
            mapOf(
                "Premain-Class" to "dev.byzann.hyperlaunch.agent.LauncherAgentKt",
                "Agent-Class" to "dev.byzann.hyperlaunch.agent.LauncherAgentKt",
                "Can-Redefine-Classes" to "true",
                "Can-Retransform-Classes" to "true"
            )
        )
    }
    // Fat JAR — agent must be self-contained
    from({
        configurations.runtimeClasspath.get().map { if (it.isDirectory) it else zipTree(it) }
    })
    duplicatesStrategy = DuplicatesStrategy.EXCLUDE
}