import type { ReactNode } from "react";
import clsx from "clsx";
import Heading from "@theme/Heading";
import styles from "./styles.module.css";

type FeatureItem = {
  title: string;
  Svg: React.ComponentType<React.ComponentProps<"svg">>;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: "Easy to Use",
    Svg: require("@site/static/img/easy-to-use.svg").default,
    description: (
      <>
        Aevatar provides a plug-and-play architecture, allowing developers to
        quickly build distributed, event-driven applications with minimal
        configuration and clear APIs.
      </>
    ),
  },
  {
    title: "Focused on What Matters",
    Svg: require("@site/static/img/focus.svg").default,
    description: (
      <>
        By abstracting away infrastructure complexity, Aevatar lets you
        concentrate on your business logic, not boilerplate or plumbing code.
      </>
    ),
  },
  {
    title: "Powered by Orleans / Virtual Actor Model",
    Svg: require("@site/static/img/powered by.svg").default,
    description: (
      <>
        Built on Microsoft Orleans, Aevatar leverages the Virtual Actor Model
        for high scalability, reliability, and seamless state management in
        distributed systems.
      </>
    ),
  },
];

function Feature({ title, Svg, description }: FeatureItem) {
  return (
    <div className={clsx("col col--4")}>
      <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
